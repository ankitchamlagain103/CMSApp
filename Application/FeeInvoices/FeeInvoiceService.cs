using Application.Common.Helpers;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.FeeInvoices.Commands;
using Application.FeeInvoices.Dtos;
using Application.FeeInvoices.Queries;
using Application.FeeInvoices.Validators;
using Domain.Common.Filters;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using FluentValidation.Results;

namespace Application.FeeInvoices
{
    public class FeeInvoiceService : IFeeInvoiceService
    {
        private const string DueDayConfigParam = "FEE_DUE_DAY_OF_MONTH";
        private const int DefaultDueDay = 10;

        private readonly IUnitOfWork _unitOfWork;
        private readonly GenerateFeeInvoicesCommandValidator _generateValidator;
        private readonly UpdateFeeInvoiceCommandValidator _updateValidator;
        private readonly FeeInvoiceLineInputValidator _lineInputValidator;
        private readonly UpdateFeeInvoiceLineCommandValidator _updateLineValidator;
        private readonly CreateFeeAdjustmentCommandValidator _createAdjustmentValidator;
        private readonly UpdateFeeAdjustmentCommandValidator _updateAdjustmentValidator;
        private readonly CreateBulkFeeAdjustmentCommandValidator _createBulkAdjustmentValidator;

        public FeeInvoiceService(
            IUnitOfWork unitOfWork,
            GenerateFeeInvoicesCommandValidator generateValidator,
            UpdateFeeInvoiceCommandValidator updateValidator,
            FeeInvoiceLineInputValidator lineInputValidator,
            UpdateFeeInvoiceLineCommandValidator updateLineValidator,
            CreateFeeAdjustmentCommandValidator createAdjustmentValidator,
            UpdateFeeAdjustmentCommandValidator updateAdjustmentValidator,
            CreateBulkFeeAdjustmentCommandValidator createBulkAdjustmentValidator)
        {
            _unitOfWork = unitOfWork;
            _generateValidator = generateValidator;
            _updateValidator = updateValidator;
            _lineInputValidator = lineInputValidator;
            _updateLineValidator = updateLineValidator;
            _createAdjustmentValidator = createAdjustmentValidator;
            _updateAdjustmentValidator = updateAdjustmentValidator;
            _createBulkAdjustmentValidator = createBulkAdjustmentValidator;
        }

        // One generation run over an academic year's Enrolled enrollments -- all Draft invoices
        // are created in a single SaveChangesAsync (all-or-nothing), and every skipped
        // enrollment is reported with its reason instead of failing the whole run.
        public async Task<CommonResponse<FeeGenerationResultDto>> GenerateAsync(GenerateFeeInvoicesCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _generateValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<FeeGenerationResultDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var academicYear = await _unitOfWork.AcademicYears.GetByIdAsync(command.AcademicYearId, cancellationToken);
            if (academicYear == null)
            {
                var notFoundResponse = CommonResponse<FeeGenerationResultDto>.Fail(ResponseCodes.NotFound, "Academic year with id '" + command.AcademicYearId + "' was not found.");
                return notFoundResponse;
            }

            if (command.AcademicClassId.HasValue)
            {
                var academicClass = await _unitOfWork.AcademicClasses.GetByIdAsync(command.AcademicClassId.Value, cancellationToken);
                if (academicClass == null)
                {
                    var classNotFoundResponse = CommonResponse<FeeGenerationResultDto>.Fail(ResponseCodes.NotFound, "Academic class with id '" + command.AcademicClassId.Value + "' was not found.");
                    return classNotFoundResponse;
                }

                if (academicClass.AcademicYearId != command.AcademicYearId)
                {
                    var classWrongYearResponse = CommonResponse<FeeGenerationResultDto>.Fail(ResponseCodes.ValidationError, "That class does not belong to the given academic year.");
                    return classWrongYearResponse;
                }
            }

            if (command.ClassSectionId.HasValue)
            {
                var section = await _unitOfWork.AcademicClasses.GetSectionByIdAsync(command.ClassSectionId.Value, cancellationToken);
                if (section == null)
                {
                    var sectionNotFoundResponse = CommonResponse<FeeGenerationResultDto>.Fail(ResponseCodes.NotFound, "Class section with id '" + command.ClassSectionId.Value + "' was not found.");
                    return sectionNotFoundResponse;
                }

                if (command.AcademicClassId.HasValue && section.AcademicClassId != command.AcademicClassId.Value)
                {
                    var wrongClassResponse = CommonResponse<FeeGenerationResultDto>.Fail(ResponseCodes.ValidationError, "That class section does not belong to the given class.");
                    return wrongClassResponse;
                }

                var sectionClass = await _unitOfWork.AcademicClasses.GetByIdAsync(section.AcademicClassId, cancellationToken);
                if (sectionClass == null || sectionClass.AcademicYearId != command.AcademicYearId)
                {
                    var wrongYearResponse = CommonResponse<FeeGenerationResultDto>.Fail(ResponseCodes.ValidationError, "That class section does not belong to the given academic year.");
                    return wrongYearResponse;
                }
            }

            var enrollments = await _unitOfWork.Enrollments.GetEnrolledByYearAsync(command.AcademicYearId, command.AcademicClassId, command.ClassSectionId, cancellationToken);

            var result = new FeeGenerationResultDto
            {
                BillingYear = command.BillingYear,
                BillingMonth = command.BillingMonth
            };

            // 2026-07-21: skip reasons are counted, not listed one row per enrollment -- a
            // regenerate call over an already-fully-generated month used to return hundreds of
            // near-identical rows.
            var skipReasonCounts = new Dictionary<string, int>();

            if (enrollments.Count == 0)
            {
                var emptyResponse = CommonResponse<FeeGenerationResultDto>.Success(result, "No enrolled students were found for that scope.");
                return emptyResponse;
            }

            // Period-keyed master record (the fee-side FeeGenerationRun): one live row per
            // (AcademicYear, BillingYear, BillingMonth), found-or-created here so multiple scoped
            // generate calls for the same period (one class at a time, or "all classes") all land
            // on the same run. Its class/student/invoice breakdown is read live from FeeInvoice,
            // not stored.
            var feeGenerationRun = await _unitOfWork.FeeGenerationRuns.GetByPeriodAsync(command.AcademicYearId, command.BillingYear, command.BillingMonth, cancellationToken);
            if (feeGenerationRun == null)
            {
                feeGenerationRun = new FeeGenerationRun
                {
                    Id = Guid.NewGuid(),
                    AcademicYearId = command.AcademicYearId,
                    BillingYear = command.BillingYear,
                    BillingMonth = command.BillingMonth,
                    GeneratedTs = DateTime.UtcNow
                };
                await _unitOfWork.FeeGenerationRuns.AddAsync(feeGenerationRun, cancellationToken);
            }
            else
            {
                feeGenerationRun.LastRegeneratedTs = DateTime.UtcNow;
            }

            result.FeeGenerationRunId = feeGenerationRun.Id;

            var enrollmentIds = new List<Guid>();
            foreach (var enrollment in enrollments)
            {
                enrollmentIds.Add(enrollment.Id);
            }

            // With Lines: BuildInvoice's Annual branch needs earlier invoices' actual
            // AnnualInstallment amounts to compute each item's remaining balance.
            var existingInvoices = await _unitOfWork.FeeInvoices.GetByEnrollmentIdsWithLinesAsync(enrollmentIds, cancellationToken);
            var invoicesByEnrollment = new Dictionary<Guid, List<FeeInvoice>>();
            foreach (var invoice in existingInvoices)
            {
                if (!invoicesByEnrollment.TryGetValue(invoice.EnrollmentId, out var enrollmentInvoices))
                {
                    enrollmentInvoices = new List<FeeInvoice>();
                    invoicesByEnrollment[invoice.EnrollmentId] = enrollmentInvoices;
                }

                enrollmentInvoices.Add(invoice);
            }

            // Pre-pass: find each enrollment's existing invoice (if any) for this exact billing
            // period, and which of those are Draft invoices about to be replaced this run. Done
            // up front -- rather than discovered inside the main loop below, as this used to work
            // -- so the Applied adjustments those Drafts already carry can be reverted to Pending
            // and merged into adjustmentsByEnrollment BEFORE it's built. Building it the old way
            // (revert-after-the-loop) meant a regenerated invoice silently missed an adjustment
            // that was already folded into the Draft it replaces: GetPendingAdjustmentsForPeriodAsync
            // only returns rows still Pending in the database, and an in-memory-only status flip
            // performed after generation isn't visible to that query without an interim save,
            // which would break the "one all-or-nothing SaveChangesAsync" guarantee.
            var existingForPeriodByEnrollment = new Dictionary<Guid, FeeInvoice>();
            var regeneratedDraftIds = new List<Guid>();
            foreach (var enrollment in enrollments)
            {
                invoicesByEnrollment.TryGetValue(enrollment.Id, out var enrollmentInvoicesForScan);
                if (enrollmentInvoicesForScan == null)
                {
                    continue;
                }

                FeeInvoice existingForPeriod = null;
                foreach (var invoice in enrollmentInvoicesForScan)
                {
                    if (invoice.BillingYear == command.BillingYear && invoice.BillingMonth == command.BillingMonth)
                    {
                        existingForPeriod = invoice;
                    }
                }

                if (existingForPeriod == null)
                {
                    continue;
                }

                existingForPeriodByEnrollment[enrollment.Id] = existingForPeriod;

                if (existingForPeriod.Status == FeeInvoiceStatus.Draft && command.RegenerateDrafts)
                {
                    regeneratedDraftIds.Add(existingForPeriod.Id);
                }
            }

            var discountsByEnrollment = await LoadDiscountsByEnrollmentAsync(enrollmentIds, cancellationToken);
            var scholarshipsByEnrollment = await LoadScholarshipsByEnrollmentAsync(enrollmentIds, cancellationToken);
            var selectionsByEnrollment = await LoadFeeSelectionsByEnrollmentAsync(enrollmentIds, cancellationToken);

            var pendingAdjustments = await _unitOfWork.FeeInvoices.GetPendingAdjustmentsForPeriodAsync(enrollmentIds, command.BillingYear, command.BillingMonth, cancellationToken);
            var adjustmentsByEnrollment = new Dictionary<Guid, List<FeeAdjustment>>();
            foreach (var adjustment in pendingAdjustments)
            {
                if (!adjustmentsByEnrollment.TryGetValue(adjustment.EnrollmentId, out var enrollmentAdjustments))
                {
                    enrollmentAdjustments = new List<FeeAdjustment>();
                    adjustmentsByEnrollment[adjustment.EnrollmentId] = enrollmentAdjustments;
                }

                enrollmentAdjustments.Add(adjustment);
            }

            if (regeneratedDraftIds.Count > 0)
            {
                var appliedAdjustments = await _unitOfWork.FeeInvoices.GetAdjustmentsAppliedToInvoicesAsync(regeneratedDraftIds, cancellationToken);
                foreach (var appliedAdjustment in appliedAdjustments)
                {
                    appliedAdjustment.Status = AdjustmentStatus.Pending;
                    appliedAdjustment.AppliedFeeInvoiceId = null;

                    if (!adjustmentsByEnrollment.TryGetValue(appliedAdjustment.EnrollmentId, out var revivedAdjustments))
                    {
                        revivedAdjustments = new List<FeeAdjustment>();
                        adjustmentsByEnrollment[appliedAdjustment.EnrollmentId] = revivedAdjustments;
                    }

                    revivedAdjustments.Add(appliedAdjustment);
                }
            }

            var feeStructuresByClass = new Dictionary<Guid, FeeStructure>();
            foreach (var enrollment in enrollments)
            {
                var academicClassId = enrollment.ClassSection.AcademicClassId;
                if (feeStructuresByClass.ContainsKey(academicClassId))
                {
                    continue;
                }

                var feeStructure = await _unitOfWork.FeeStructures.GetByAcademicClassIdAsync(academicClassId, cancellationToken);
                feeStructuresByClass[academicClassId] = feeStructure;
            }

            var dueDate = await ResolveDueDateAsync(command.BillingYear, command.BillingMonth, cancellationToken);
            var feeLabels = await LoadFeeLabelMapAsync(cancellationToken);

            var invoiceNoPrefix = FeeInvoiceFactory.InvoiceNoPrefix + command.BillingYear;
            var existingInvoiceNos = await _unitOfWork.FeeInvoices.GetInvoiceNosByPrefixAsync(invoiceNoPrefix, cancellationToken);
            var invoiceNos = new List<string>(existingInvoiceNos);

            foreach (var enrollment in enrollments)
            {
                invoicesByEnrollment.TryGetValue(enrollment.Id, out var enrollmentInvoices);
                if (enrollmentInvoices == null)
                {
                    enrollmentInvoices = new List<FeeInvoice>();
                }

                existingForPeriodByEnrollment.TryGetValue(enrollment.Id, out var existingForPeriod);

                if (existingForPeriod != null)
                {
                    if (existingForPeriod.Status != FeeInvoiceStatus.Draft)
                    {
                        IncrementSkipReason(skipReasonCounts, "Invoice Already Generated");
                        continue;
                    }

                    if (!command.RegenerateDrafts)
                    {
                        IncrementSkipReason(skipReasonCounts, "Draft Invoice Already Exists");
                        continue;
                    }

                    _unitOfWork.FeeInvoices.Remove(existingForPeriod);
                    enrollmentInvoices.Remove(existingForPeriod);
                }

                var academicClassId = enrollment.ClassSection.AcademicClassId;
                var feeStructure = feeStructuresByClass[academicClassId];
                if (feeStructure == null)
                {
                    IncrementSkipReason(skipReasonCounts, "No Fee Structure Configured");
                    continue;
                }

                if (feeStructure.Status != RecordStatus.Active)
                {
                    IncrementSkipReason(skipReasonCounts, "Fee Structure Not Active");
                    continue;
                }

                discountsByEnrollment.TryGetValue(enrollment.Id, out var discounts);
                scholarshipsByEnrollment.TryGetValue(enrollment.Id, out var scholarships);
                selectionsByEnrollment.TryGetValue(enrollment.Id, out var selectedItemIds);
                adjustmentsByEnrollment.TryGetValue(enrollment.Id, out var adjustments);

                var newInvoiceId = Guid.NewGuid();
                adjustments = await EnsureCarryForwardAdjustmentAsync(enrollment.Id, enrollmentInvoices, adjustments, existingForPeriod, newInvoiceId, command.BillingYear, command.BillingMonth, cancellationToken);

                var invoiceNo = NumberSequenceHelper.Next(invoiceNoPrefix, invoiceNos, 4);
                invoiceNos.Add(invoiceNo);

                var newInvoice = FeeInvoiceFactory.BuildInvoice(
                    newInvoiceId,
                    enrollment,
                    academicYear,
                    feeStructure,
                    enrollmentInvoices,
                    discounts ?? new List<StudentDiscount>(),
                    scholarships ?? new List<StudentScholarship>(),
                    selectedItemIds ?? new List<Guid>(),
                    adjustments ?? new List<FeeAdjustment>(),
                    command.BillingYear,
                    command.BillingMonth,
                    dueDate,
                    invoiceNo,
                    stampAdjustments: true,
                    feeLabels);

                await _unitOfWork.FeeInvoices.AddAsync(newInvoice, cancellationToken);
                result.GeneratedInvoiceIds.Add(newInvoice.Id);
                result.GeneratedCount++;
            }

            foreach (var reasonAndCount in skipReasonCounts)
            {
                var skipSummary = new FeeGenerationSkipSummaryDto
                {
                    Reason = reasonAndCount.Key,
                    Count = reasonAndCount.Value
                };
                result.SkippedSummary.Add(skipSummary);
                result.SkippedCount += reasonAndCount.Value;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var successResponse = CommonResponse<FeeGenerationResultDto>.Success(result, "Generated " + result.GeneratedCount + " draft invoice(s); skipped " + result.SkippedCount + ".");
            return successResponse;
        }

        private static void IncrementSkipReason(Dictionary<string, int> skipReasonCounts, string reason)
        {
            skipReasonCounts.TryGetValue(reason, out var currentCount);
            skipReasonCounts[reason] = currentCount + 1;
        }

        // Auto-carries a strictly-earlier-period outstanding balance forward as a Pending
        // CARRY_CORRECTION FeeAdjustment, which BuildInvoice folds into the new invoice through
        // the same pending-adjustment mechanism every other adjustment already uses -- no changes
        // needed there. Every contributing invoice is voided (Status -> Cancelled) at the same
        // moment, with CarriedForwardToInvoiceId pointing at the new invoice -- that Cancelled
        // status alone is what excludes it from every outstanding-balance sum and the
        // account-statement ledger going forward; CarriedForwardAmount is kept purely for
        // display. Any of its own Applied adjustments are re-pended first (same "voiding an
        // invoice re-pends its adjustments" convention as CancelAsync/regenerate).
        //
        // Regenerate case: existingForPeriod is the Draft about to be replaced. If it already
        // carries a Pending CARRY_CORRECTION (revived into `adjustments` by the caller's
        // adjustment re-pend step), the invoices that fed it are already Cancelled and therefore
        // no longer present in enrollmentInvoices -- there is nothing left to recompute against,
        // so the adjustment's Value is left untouched and the voided invoice(s) just have their
        // reference repointed from the replaced Draft's id to its replacement's id.
        private async Task<List<FeeAdjustment>> EnsureCarryForwardAdjustmentAsync(
            Guid enrollmentId,
            IReadOnlyList<FeeInvoice> enrollmentInvoices,
            List<FeeAdjustment> adjustments,
            FeeInvoice existingForPeriod,
            Guid newInvoiceId,
            int billingYear,
            int billingMonth,
            CancellationToken cancellationToken)
        {
            FeeAdjustment existingCarryAdjustment = null;
            if (adjustments != null)
            {
                foreach (var existingAdjustment in adjustments)
                {
                    if (existingAdjustment.AdjustmentTypeCode == FeeAdjustmentTypeCodes.CarryCorrection)
                    {
                        existingCarryAdjustment = existingAdjustment;
                    }
                }
            }

            if (existingCarryAdjustment != null)
            {
                if (existingForPeriod != null)
                {
                    var carriedInvoices = await _unitOfWork.FeeInvoices.GetByCarriedForwardTargetAsync(existingForPeriod.Id, cancellationToken);
                    foreach (var carriedInvoice in carriedInvoices)
                    {
                        carriedInvoice.CarriedForwardToInvoiceId = newInvoiceId;
                    }
                }

                return adjustments;
            }

            decimal outstandingBeforePeriod = 0m;
            var contributingInvoices = new List<FeeInvoice>();

            foreach (var otherInvoice in enrollmentInvoices)
            {
                if (otherInvoice.Status == FeeInvoiceStatus.Draft || otherInvoice.Status == FeeInvoiceStatus.Cancelled)
                {
                    continue;
                }

                if (!IsBillingPeriodStrictlyBefore(otherInvoice.BillingYear, otherInvoice.BillingMonth, billingYear, billingMonth))
                {
                    continue;
                }

                var remaining = otherInvoice.NetAmount - otherInvoice.PaidAmount;
                if (remaining <= 0m)
                {
                    continue;
                }

                outstandingBeforePeriod += remaining;
                contributingInvoices.Add(otherInvoice);
            }

            if (outstandingBeforePeriod <= 0m)
            {
                return adjustments;
            }

            var contributingInvoiceIds = new List<Guid>();
            var contributingInvoiceNos = new List<string>();
            foreach (var contributingInvoice in contributingInvoices)
            {
                contributingInvoiceIds.Add(contributingInvoice.Id);
                contributingInvoiceNos.Add(contributingInvoice.InvoiceNo);
            }

            var appliedAdjustmentsOnContributors = await _unitOfWork.FeeInvoices.GetAdjustmentsAppliedToInvoicesAsync(contributingInvoiceIds, cancellationToken);
            foreach (var appliedAdjustment in appliedAdjustmentsOnContributors)
            {
                appliedAdjustment.Status = AdjustmentStatus.Pending;
                appliedAdjustment.AppliedFeeInvoiceId = null;
            }

            var carryAdjustment = new FeeAdjustment
            {
                Id = Guid.NewGuid(),
                EnrollmentId = enrollmentId,
                BillingYear = billingYear,
                BillingMonth = billingMonth,
                AdjustmentTypeCode = FeeAdjustmentTypeCodes.CarryCorrection,
                Direction = AdjustmentDirection.Increase,
                ValueType = AwardValueType.FixedAmount,
                Value = outstandingBeforePeriod,
                Remarks = "Carried forward from " + string.Join(", ", contributingInvoiceNos),
                Status = AdjustmentStatus.Pending
            };

            await _unitOfWork.FeeInvoices.AddAdjustmentAsync(carryAdjustment, cancellationToken);

            if (adjustments == null)
            {
                adjustments = new List<FeeAdjustment>();
            }

            adjustments.Add(carryAdjustment);

            foreach (var contributingInvoice in contributingInvoices)
            {
                contributingInvoice.CarriedForwardAmount = contributingInvoice.NetAmount - contributingInvoice.PaidAmount;
                contributingInvoice.CarriedForwardToInvoiceId = newInvoiceId;
                contributingInvoice.Status = FeeInvoiceStatus.Cancelled;
            }

            return adjustments;
        }

        private static bool IsBillingPeriodStrictlyBefore(int year, int month, int targetYear, int targetMonth)
        {
            if (year != targetYear)
            {
                return year < targetYear;
            }

            return month < targetMonth;
        }

        public async Task<CommonResponse<PaginatedResponse<FeeInvoiceDto>>> GetFeeInvoicesAsync(GetFeeInvoicesQuery query, CancellationToken cancellationToken = default)
        {
            var filter = new FeeInvoiceFilter
            {
                AcademicYearId = query.AcademicYearId,
                BillingYear = query.BillingYear,
                BillingMonth = query.BillingMonth,
                AcademicClassId = query.AcademicClassId,
                ClassSectionId = query.ClassSectionId,
                EnrollmentId = query.EnrollmentId,
                Status = query.Status,
                Search = query.Search
            };

            var pagedInvoices = await _unitOfWork.FeeInvoices.GetPagedByFilterAsync(filter, query.Page, query.PageSize, cancellationToken);

            // Overdue-status stamping is opportunistic (self-healing on read, like EmployeeLoan
            // auto-close) -- no scheduler exists to do it on time.
            var anyStatusChanged = RefreshDerivedStatuses(pagedInvoices.Items);
            if (anyStatusChanged)
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            var invoiceDtos = new List<FeeInvoiceDto>();
            foreach (var invoice in pagedInvoices.Items)
            {
                var invoiceDto = FeeInvoiceMapper.ToDto(invoice, includeLines: false);
                invoiceDtos.Add(invoiceDto);
            }

            var paginatedResponse = new PaginatedResponse<FeeInvoiceDto>
            {
                Items = invoiceDtos,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = pagedInvoices.TotalCount
            };

            var successResponse = CommonResponse<PaginatedResponse<FeeInvoiceDto>>.Success(paginatedResponse);
            return successResponse;
        }

        public async Task<CommonResponse<FeeInvoiceDto>> GetFeeInvoiceByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var invoice = await _unitOfWork.FeeInvoices.GetByIdWithLinesAsync(id, cancellationToken);
            if (invoice == null)
            {
                var notFoundResponse = CommonResponse<FeeInvoiceDto>.Fail(ResponseCodes.NotFound, "Fee invoice with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            var statusChanged = RefreshDerivedStatuses(new List<FeeInvoice> { invoice });
            if (statusChanged)
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            var invoiceDto = await MapInvoiceWithLabelsAsync(invoice, cancellationToken);
            var successResponse = CommonResponse<FeeInvoiceDto>.Success(invoiceDto);
            return successResponse;
        }

        public async Task<CommonResponse<FeeInvoiceDto>> UpdateFeeInvoiceAsync(Guid id, UpdateFeeInvoiceCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _updateValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<FeeInvoiceDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var invoice = await _unitOfWork.FeeInvoices.GetByIdWithLinesAsync(id, cancellationToken);
            if (invoice == null)
            {
                var notFoundResponse = CommonResponse<FeeInvoiceDto>.Fail(ResponseCodes.NotFound, "Fee invoice with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            if (invoice.Status != FeeInvoiceStatus.Draft)
            {
                var lockedResponse = CommonResponse<FeeInvoiceDto>.Fail(ResponseCodes.Conflict, "Only Draft invoices can be edited; this one is '" + invoice.Status + "'.");
                return lockedResponse;
            }

            invoice.DueDate = DateTimeHelper.AsUtcDate(command.DueDate);
            invoice.Remarks = command.Remarks?.Trim();

            _unitOfWork.FeeInvoices.Update(invoice);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var invoiceDto = await MapInvoiceWithLabelsAsync(invoice, cancellationToken);
            var successResponse = CommonResponse<FeeInvoiceDto>.Success(invoiceDto, "Fee invoice updated successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<FeeInvoiceDto>> AddLineAsync(Guid invoiceId, FeeInvoiceLineInput command, CancellationToken cancellationToken = default)
        {
            var validationResult = _lineInputValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<FeeInvoiceDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var invoice = await _unitOfWork.FeeInvoices.GetByIdWithLinesAsync(invoiceId, cancellationToken);
            if (invoice == null)
            {
                var notFoundResponse = CommonResponse<FeeInvoiceDto>.Fail(ResponseCodes.NotFound, "Fee invoice with id '" + invoiceId + "' was not found.");
                return notFoundResponse;
            }

            if (invoice.Status != FeeInvoiceStatus.Draft)
            {
                var lockedResponse = CommonResponse<FeeInvoiceDto>.Fail(ResponseCodes.Conflict, "Only Draft invoices can be edited; this one is '" + invoice.Status + "'.");
                return lockedResponse;
            }

            var categoryCode = command.FeeCategoryCode?.Trim();
            if (!string.IsNullOrWhiteSpace(categoryCode))
            {
                var categoryExists = await _unitOfWork.Configs.CodeExistsAsync(ConfigTypeCodes.FeeCategory, categoryCode, cancellationToken);
                if (!categoryExists)
                {
                    var invalidCategoryResponse = CommonResponse<FeeInvoiceDto>.Fail(ResponseCodes.ValidationError, "FeeCategoryCode '" + categoryCode + "' is not a known fee category option.");
                    return invalidCategoryResponse;
                }
            }
            else
            {
                categoryCode = null;
            }

            var line = new FeeInvoiceLine
            {
                FeeInvoiceId = invoiceId,
                Source = FeeLineSource.Manual,
                FeeCategoryCode = categoryCode,
                Description = command.Description.Trim(),
                Amount = command.Amount,
                FeeInvoice = invoice
            };

            invoice.Lines.Add(line);
            await _unitOfWork.FeeInvoices.AddLineAsync(line, cancellationToken);

            FeeInvoiceFactory.RecomputeTotals(invoice);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var invoiceDto = await MapInvoiceWithLabelsAsync(invoice, cancellationToken);
            var successResponse = CommonResponse<FeeInvoiceDto>.Success(invoiceDto, "Line added successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<FeeInvoiceDto>> UpdateLineAsync(Guid invoiceId, Guid lineId, UpdateFeeInvoiceLineCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _updateLineValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<FeeInvoiceDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var invoice = await _unitOfWork.FeeInvoices.GetByIdWithLinesAsync(invoiceId, cancellationToken);
            if (invoice == null)
            {
                var notFoundResponse = CommonResponse<FeeInvoiceDto>.Fail(ResponseCodes.NotFound, "Fee invoice with id '" + invoiceId + "' was not found.");
                return notFoundResponse;
            }

            if (invoice.Status != FeeInvoiceStatus.Draft)
            {
                var lockedResponse = CommonResponse<FeeInvoiceDto>.Fail(ResponseCodes.Conflict, "Only Draft invoices can be edited; this one is '" + invoice.Status + "'.");
                return lockedResponse;
            }

            FeeInvoiceLine line = null;
            foreach (var invoiceLine in invoice.Lines)
            {
                if (invoiceLine.Id == lineId)
                {
                    line = invoiceLine;
                }
            }

            if (line == null)
            {
                var lineNotFoundResponse = CommonResponse<FeeInvoiceDto>.Fail(ResponseCodes.NotFound, "Line was not found on this invoice.");
                return lineNotFoundResponse;
            }

            line.Description = command.Description.Trim();
            line.Amount = command.Amount;

            FeeInvoiceFactory.RecomputeTotals(invoice);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var invoiceDto = await MapInvoiceWithLabelsAsync(invoice, cancellationToken);
            var successResponse = CommonResponse<FeeInvoiceDto>.Success(invoiceDto, "Line updated successfully.");
            return successResponse;
        }

        // "Pay this Annual fee in full now" (2026-07-17) -- a one-click alternative to hand-
        // editing an AnnualInstallment line's Amount. Sets the line to the item's TRUE
        // remaining balance (the full annual amount minus whatever's already billed on the
        // enrollment's OTHER invoices, this line's own current amount excluded from that sum
        // since it's about to be replaced). No separate "fully paid" flag is written anywhere:
        // FeeInvoiceFactory's Annual branch is remaining-balance-driven, so every later month
        // reads this bigger line amount back out of history and sees nothing left to bill.
        public async Task<CommonResponse<FeeInvoiceDto>> SettleAnnualInFullAsync(Guid invoiceId, Guid lineId, CancellationToken cancellationToken = default)
        {
            var invoice = await _unitOfWork.FeeInvoices.GetByIdWithLinesAsync(invoiceId, cancellationToken);
            if (invoice == null)
            {
                var notFoundResponse = CommonResponse<FeeInvoiceDto>.Fail(ResponseCodes.NotFound, "Fee invoice with id '" + invoiceId + "' was not found.");
                return notFoundResponse;
            }

            if (invoice.Status != FeeInvoiceStatus.Draft)
            {
                var lockedResponse = CommonResponse<FeeInvoiceDto>.Fail(ResponseCodes.Conflict, "Only Draft invoices can be edited; this one is '" + invoice.Status + "'.");
                return lockedResponse;
            }

            FeeInvoiceLine line = null;
            foreach (var invoiceLine in invoice.Lines)
            {
                if (invoiceLine.Id == lineId)
                {
                    line = invoiceLine;
                }
            }

            if (line == null)
            {
                var lineNotFoundResponse = CommonResponse<FeeInvoiceDto>.Fail(ResponseCodes.NotFound, "Line was not found on this invoice.");
                return lineNotFoundResponse;
            }

            if (line.Source != FeeLineSource.AnnualInstallment || !line.FeeStructureItemId.HasValue)
            {
                var wrongSourceResponse = CommonResponse<FeeInvoiceDto>.Fail(ResponseCodes.ValidationError, "Only an Annual fee installment line can be settled in full.");
                return wrongSourceResponse;
            }

            var item = await _unitOfWork.FeeStructures.GetItemByIdAsync(line.FeeStructureItemId.Value, cancellationToken);
            if (item == null)
            {
                var itemNotFoundResponse = CommonResponse<FeeInvoiceDto>.Fail(ResponseCodes.NotFound, "The fee structure item behind this line no longer exists.");
                return itemNotFoundResponse;
            }

            var otherInvoices = await _unitOfWork.FeeInvoices.GetByEnrollmentIdsWithLinesAsync(new List<Guid> { invoice.EnrollmentId }, cancellationToken);

            decimal alreadyBilledElsewhere = 0m;
            foreach (var otherInvoice in otherInvoices)
            {
                if (otherInvoice.Id == invoice.Id)
                {
                    continue;
                }

                foreach (var otherLine in otherInvoice.Lines)
                {
                    if (otherLine.Source == FeeLineSource.AnnualInstallment && otherLine.FeeStructureItemId == item.Id)
                    {
                        alreadyBilledElsewhere += otherLine.Amount;
                    }
                }
            }

            var remainingForItem = item.Amount - alreadyBilledElsewhere;
            if (remainingForItem <= 0m)
            {
                var nothingRemainingResponse = CommonResponse<FeeInvoiceDto>.Fail(ResponseCodes.Conflict, "This item is already fully billed across this enrollment's other invoices -- nothing remains to settle here.");
                return nothingRemainingResponse;
            }

            line.Amount = remainingForItem;
            line.Description = item.FeeCategoryCode + " (Annual, paid in full)";

            FeeInvoiceFactory.RecomputeTotals(invoice);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var invoiceDto = await MapInvoiceWithLabelsAsync(invoice, cancellationToken);
            var successResponse = CommonResponse<FeeInvoiceDto>.Success(invoiceDto, "Annual fee settled in full on this invoice (" + remainingForItem.ToString("F2") + "); future installments for this item will stop automatically.");
            return successResponse;
        }

        public async Task<CommonResponse<FeeInvoiceDto>> RemoveLineAsync(Guid invoiceId, Guid lineId, CancellationToken cancellationToken = default)
        {
            var invoice = await _unitOfWork.FeeInvoices.GetByIdWithLinesAsync(invoiceId, cancellationToken);
            if (invoice == null)
            {
                var notFoundResponse = CommonResponse<FeeInvoiceDto>.Fail(ResponseCodes.NotFound, "Fee invoice with id '" + invoiceId + "' was not found.");
                return notFoundResponse;
            }

            if (invoice.Status != FeeInvoiceStatus.Draft)
            {
                var lockedResponse = CommonResponse<FeeInvoiceDto>.Fail(ResponseCodes.Conflict, "Only Draft invoices can be edited; this one is '" + invoice.Status + "'.");
                return lockedResponse;
            }

            FeeInvoiceLine line = null;
            foreach (var invoiceLine in invoice.Lines)
            {
                if (invoiceLine.Id == lineId)
                {
                    line = invoiceLine;
                }
            }

            if (line == null)
            {
                var lineNotFoundResponse = CommonResponse<FeeInvoiceDto>.Fail(ResponseCodes.NotFound, "Line was not found on this invoice.");
                return lineNotFoundResponse;
            }

            // A removed MonthlyAdjustment line releases its source adjustment back to Pending
            // so the admin's entry isn't silently lost.
            if (line.FeeAdjustmentId.HasValue)
            {
                var sourceAdjustment = await _unitOfWork.FeeInvoices.GetAdjustmentByIdAsync(line.FeeAdjustmentId.Value, cancellationToken);
                if (sourceAdjustment != null && sourceAdjustment.Status == AdjustmentStatus.Applied)
                {
                    sourceAdjustment.Status = AdjustmentStatus.Pending;
                    sourceAdjustment.AppliedFeeInvoiceId = null;
                }
            }

            invoice.Lines.Remove(line);
            _unitOfWork.FeeInvoices.RemoveLine(line);

            FeeInvoiceFactory.RecomputeTotals(invoice);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var invoiceDto = await MapInvoiceWithLabelsAsync(invoice, cancellationToken);
            var successResponse = CommonResponse<FeeInvoiceDto>.Success(invoiceDto, "Line removed successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<FinalizeResultDto>> FinalizeAsync(FinalizeFeeInvoicesCommand command, CancellationToken cancellationToken = default)
        {
            if (command.InvoiceIds == null || command.InvoiceIds.Count == 0)
            {
                var emptyResponse = CommonResponse<FinalizeResultDto>.Fail(ResponseCodes.ValidationError, "InvoiceIds must contain at least one invoice id.");
                return emptyResponse;
            }

            var invoices = await _unitOfWork.FeeInvoices.GetByIdsWithLinesAsync(command.InvoiceIds, cancellationToken);

            var result = new FinalizeResultDto();
            var foundIds = new List<Guid>();

            foreach (var invoice in invoices)
            {
                foundIds.Add(invoice.Id);

                if (invoice.Status != FeeInvoiceStatus.Draft)
                {
                    result.SkippedInvoiceIds.Add(invoice.Id);
                    continue;
                }

                FeeInvoiceFactory.RecomputeTotals(invoice);
                invoice.Status = FeeInvoiceStatus.Generated;
                invoice.GeneratedTs = DateTime.UtcNow;
                result.FinalizedCount++;
            }

            foreach (var requestedId in command.InvoiceIds)
            {
                if (!foundIds.Contains(requestedId))
                {
                    result.SkippedInvoiceIds.Add(requestedId);
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var successResponse = CommonResponse<FinalizeResultDto>.Success(result, "Finalized " + result.FinalizedCount + " invoice(s); skipped " + result.SkippedInvoiceIds.Count + ".");
            return successResponse;
        }

        // Reverses a Finalize -- Generated/Pending(Overdue) back to Draft, editable again and
        // eligible for the next regenerate/refresh to fold in adjustments created after this
        // invoice was locked. Blocked once money has moved (PartiallyPaid/Paid), same guard
        // CancelAsync already uses -- unfinalizing an invoice a payment was allocated against
        // would desync that payment from an invoice whose amount could then change underneath it.
        public async Task<CommonResponse<FeeInvoiceDto>> UnfinalizeAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var invoice = await _unitOfWork.FeeInvoices.GetByIdWithLinesAsync(id, cancellationToken);
            if (invoice == null)
            {
                var notFoundResponse = CommonResponse<FeeInvoiceDto>.Fail(ResponseCodes.NotFound, "Fee invoice with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            if (invoice.Status == FeeInvoiceStatus.Draft)
            {
                var alreadyDraftResponse = CommonResponse<FeeInvoiceDto>.Fail(ResponseCodes.Conflict, "This invoice is already a Draft.");
                return alreadyDraftResponse;
            }

            if (invoice.Status == FeeInvoiceStatus.Cancelled)
            {
                var cancelledResponse = CommonResponse<FeeInvoiceDto>.Fail(ResponseCodes.Conflict, "A cancelled invoice cannot be unfinalized.");
                return cancelledResponse;
            }

            if (invoice.Status == FeeInvoiceStatus.PartiallyPaid || invoice.Status == FeeInvoiceStatus.Paid)
            {
                var hasMoneyResponse = CommonResponse<FeeInvoiceDto>.Fail(ResponseCodes.Conflict, "This invoice has payments against it -- void those payments first.");
                return hasMoneyResponse;
            }

            invoice.Status = FeeInvoiceStatus.Draft;
            invoice.GeneratedTs = null;

            var appliedAdjustments = await _unitOfWork.FeeInvoices.GetAdjustmentsAppliedToInvoicesAsync(new List<Guid> { id }, cancellationToken);
            foreach (var appliedAdjustment in appliedAdjustments)
            {
                appliedAdjustment.Status = AdjustmentStatus.Pending;
                appliedAdjustment.AppliedFeeInvoiceId = null;
            }

            _unitOfWork.FeeInvoices.Update(invoice);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var invoiceDto = await MapInvoiceWithLabelsAsync(invoice, cancellationToken);
            var successResponse = CommonResponse<FeeInvoiceDto>.Success(invoiceDto, "Fee invoice reverted to Draft.");
            return successResponse;
        }

        public async Task<CommonResponse<FeeInvoiceDto>> CancelAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var invoice = await _unitOfWork.FeeInvoices.GetByIdWithLinesAsync(id, cancellationToken);
            if (invoice == null)
            {
                var notFoundResponse = CommonResponse<FeeInvoiceDto>.Fail(ResponseCodes.NotFound, "Fee invoice with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            if (invoice.Status == FeeInvoiceStatus.Cancelled)
            {
                var alreadyCancelledResponse = CommonResponse<FeeInvoiceDto>.Fail(ResponseCodes.Conflict, "This invoice is already cancelled.");
                return alreadyCancelledResponse;
            }

            if (invoice.Status == FeeInvoiceStatus.PartiallyPaid || invoice.Status == FeeInvoiceStatus.Paid)
            {
                var hasMoneyResponse = CommonResponse<FeeInvoiceDto>.Fail(ResponseCodes.Conflict, "This invoice has payments against it -- void those payments first.");
                return hasMoneyResponse;
            }

            invoice.Status = FeeInvoiceStatus.Cancelled;

            var appliedAdjustments = await _unitOfWork.FeeInvoices.GetAdjustmentsAppliedToInvoicesAsync(new List<Guid> { id }, cancellationToken);
            foreach (var appliedAdjustment in appliedAdjustments)
            {
                appliedAdjustment.Status = AdjustmentStatus.Pending;
                appliedAdjustment.AppliedFeeInvoiceId = null;
            }

            _unitOfWork.FeeInvoices.Update(invoice);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var invoiceDto = await MapInvoiceWithLabelsAsync(invoice, cancellationToken);
            var successResponse = CommonResponse<FeeInvoiceDto>.Success(invoiceDto, "Fee invoice cancelled.");
            return successResponse;
        }

        public async Task<CommonResponse<FeeStatementDto>> GetStatementAsync(Guid enrollmentId, CancellationToken cancellationToken = default)
        {
            var enrollment = await _unitOfWork.Enrollments.GetWithDetailsAsync(enrollmentId, cancellationToken);
            if (enrollment == null)
            {
                var notFoundResponse = CommonResponse<FeeStatementDto>.Fail(ResponseCodes.NotFound, "Enrollment with id '" + enrollmentId + "' was not found.");
                return notFoundResponse;
            }

            var invoices = await _unitOfWork.FeeInvoices.GetStatementByEnrollmentAsync(enrollmentId, cancellationToken);

            var anyStatusChanged = RefreshDerivedStatuses(invoices);
            if (anyStatusChanged)
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            var invoiceDtos = new List<FeeInvoiceDto>();
            decimal outstandingAmount = 0m;
            foreach (var invoice in invoices)
            {
                var invoiceDto = FeeInvoiceMapper.ToDto(invoice, includeLines: false);
                invoiceDtos.Add(invoiceDto);

                if (invoice.Status != FeeInvoiceStatus.Draft)
                {
                    outstandingAmount += invoice.NetAmount - invoice.PaidAmount;
                }
            }

            var statementDto = new FeeStatementDto
            {
                EnrollmentId = enrollmentId,
                StudentName = BuildStudentName(enrollment.Student),
                AdmissionNo = enrollment.Student?.AdmissionNo,
                GradeCode = enrollment.ClassSection?.AcademicClass?.GradeCode,
                SectionCode = enrollment.ClassSection?.SectionCode,
                OutstandingAmount = outstandingAmount,
                Invoices = invoiceDtos
            };

            var successResponse = CommonResponse<FeeStatementDto>.Success(statementDto);
            return successResponse;
        }

        // The ledger view behind the Statement of Account page: invoices post debits, payments
        // post credits, chronological with a running balance. ClosingBalance is the live
        // outstanding amount (same F7 rule as GetStatementAsync -- computed across open
        // invoices, never from PreviousDueAmount snapshots).
        public async Task<CommonResponse<FeeAccountStatementDto>> GetAccountStatementAsync(Guid enrollmentId, CancellationToken cancellationToken = default)
        {
            var enrollment = await _unitOfWork.Enrollments.GetWithDetailsAsync(enrollmentId, cancellationToken);
            if (enrollment == null)
            {
                var notFoundResponse = CommonResponse<FeeAccountStatementDto>.Fail(ResponseCodes.NotFound, "Enrollment with id '" + enrollmentId + "' was not found.");
                return notFoundResponse;
            }

            var invoices = await _unitOfWork.FeeInvoices.GetStatementByEnrollmentAsync(enrollmentId, cancellationToken);

            var anyStatusChanged = RefreshDerivedStatuses(invoices);
            if (anyStatusChanged)
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            var payments = await _unitOfWork.FeeInvoices.GetPaymentsByEnrollmentAsync(enrollmentId, cancellationToken);

            var entries = new List<FeeAccountEntryDto>();

            foreach (var invoice in invoices)
            {
                if (invoice.Status == FeeInvoiceStatus.Draft)
                {
                    continue;
                }

                var billingMonthName = new DateTime(invoice.BillingYear, invoice.BillingMonth, 1).ToString("MMMM yyyy");
                var invoiceDate = invoice.GeneratedTs ?? invoice.DueDate;
                var invoiceEntry = new FeeAccountEntryDto
                {
                    Date = invoiceDate,
                    EntryType = "Invoice",
                    Reference = invoice.InvoiceNo,
                    Description = "Fee invoice for " + billingMonthName,
                    Debit = invoice.GrossAmount,
                    Credit = 0m
                };
                entries.Add(invoiceEntry);

                // Discounts/scholarships/rule-discounts/negative adjustments are posted as their
                // own credit line (instead of only being netted silently into the invoice debit
                // above) so the statement shows *that* a reduction was applied and *why* -- the
                // line's own Description already reads "Discount - Sibling Discount" etc. (set
                // at generation time in FeeInvoiceFactory). Debit above uses GrossAmount rather
                // than NetAmount specifically so this credit doesn't double-subtract.
                foreach (var line in invoice.Lines)
                {
                    if (line.Amount >= 0m)
                    {
                        continue;
                    }

                    var reductionEntry = new FeeAccountEntryDto
                    {
                        Date = invoiceDate,
                        EntryType = GetReductionEntryType(line.Source),
                        Reference = invoice.InvoiceNo,
                        Description = line.Description,
                        Debit = 0m,
                        Credit = -line.Amount
                    };
                    entries.Add(reductionEntry);
                }
            }

            foreach (var payment in payments)
            {
                var paymentEntry = new FeeAccountEntryDto
                {
                    Date = payment.PaymentDate,
                    EntryType = "Payment",
                    Reference = payment.ReceiptNo,
                    Description = "Payment received (" + payment.PaymentMode + ")" + (string.IsNullOrWhiteSpace(payment.ReferenceNo) ? string.Empty : " - Ref " + payment.ReferenceNo),
                    Debit = 0m,
                    Credit = payment.Amount
                };
                entries.Add(paymentEntry);
            }

            entries.Sort(CompareAccountEntries);

            decimal runningBalance = 0m;
            decimal totalDebit = 0m;
            decimal totalCredit = 0m;
            foreach (var entry in entries)
            {
                runningBalance += entry.Debit - entry.Credit;
                totalDebit += entry.Debit;
                totalCredit += entry.Credit;
                entry.Balance = runningBalance;
            }

            var statementDto = new FeeAccountStatementDto
            {
                EnrollmentId = enrollmentId,
                StudentId = enrollment.StudentId,
                StudentName = BuildStudentName(enrollment.Student),
                AdmissionNo = enrollment.Student?.AdmissionNo,
                Email = enrollment.Student?.Email,
                GradeCode = enrollment.ClassSection?.AcademicClass?.GradeCode,
                SectionCode = enrollment.ClassSection?.SectionCode,
                OpeningBalance = 0m,
                TotalDebit = totalDebit,
                TotalCredit = totalCredit,
                ClosingBalance = runningBalance,
                Entries = entries
            };

            var successResponse = CommonResponse<FeeAccountStatementDto>.Success(statementDto);
            return successResponse;
        }

        // The fee module's student search box: matches by name/admission no/email over active
        // enrollments and returns each match's enrollment id plus live outstanding balance --
        // the jumping-off point for statements and payment entry. Every enrolled student in
        // scope is returned, search text or not -- zero-balance students are no longer hidden
        // (2026-07-21: an academicYearId-only call, e.g. browsing the whole year's roster, was
        // silently dropping everyone who happened to owe nothing). Default (no search text)
        // ordering is still highest-outstanding-first so the worklist use case reads the same,
        // it just no longer truncates the roster. An explicit search is sorted alphabetically
        // as before. Because the sort needs every match's outstanding balance -- computed from
        // FeeInvoice, a different aggregate the enrollment query can't join against -- this
        // loads every match unpaged and paginates in memory.
        public async Task<CommonResponse<PaginatedResponse<FeeStudentSearchResultDto>>> SearchStudentsAsync(SearchFeeStudentsQuery query, CancellationToken cancellationToken = default)
        {
            var hasSearchTerm = !string.IsNullOrWhiteSpace(query.Search);
            var enrollments = await _unitOfWork.Enrollments.SearchEnrolledByStudentAllAsync(query.AcademicYearId, query.Search, cancellationToken);

            var enrollmentIds = new List<Guid>();
            foreach (var enrollment in enrollments)
            {
                enrollmentIds.Add(enrollment.Id);
            }

            var outstandingByEnrollment = new Dictionary<Guid, decimal>();
            if (enrollmentIds.Count > 0)
            {
                var invoices = await _unitOfWork.FeeInvoices.GetByEnrollmentIdsAsync(enrollmentIds, cancellationToken);
                foreach (var invoice in invoices)
                {
                    if (invoice.Status == FeeInvoiceStatus.Draft)
                    {
                        continue;
                    }

                    outstandingByEnrollment.TryGetValue(invoice.EnrollmentId, out var runningOutstanding);
                    outstandingByEnrollment[invoice.EnrollmentId] = runningOutstanding + invoice.NetAmount - invoice.PaidAmount;
                }
            }

            var resultDtos = new List<FeeStudentSearchResultDto>();
            foreach (var enrollment in enrollments)
            {
                outstandingByEnrollment.TryGetValue(enrollment.Id, out var outstandingAmount);

                var resultDto = new FeeStudentSearchResultDto
                {
                    EnrollmentId = enrollment.Id,
                    StudentId = enrollment.StudentId,
                    StudentName = BuildStudentName(enrollment.Student),
                    AdmissionNo = enrollment.Student?.AdmissionNo,
                    Email = enrollment.Student?.Email,
                    Phone = enrollment.Student?.Phone,
                    AcademicYearId = enrollment.ClassSection?.AcademicClass?.AcademicYearId ?? Guid.Empty,
                    GradeCode = enrollment.ClassSection?.AcademicClass?.GradeCode,
                    SectionCode = enrollment.ClassSection?.SectionCode,
                    OutstandingAmount = outstandingAmount
                };
                resultDtos.Add(resultDto);
            }

            if (!hasSearchTerm)
            {
                resultDtos.Sort(CompareByOutstandingDescending);
            }

            var totalCount = resultDtos.Count;
            var skipCount = (query.Page - 1) * query.PageSize;
            var pagedDtos = new List<FeeStudentSearchResultDto>();
            for (var index = skipCount; index < resultDtos.Count && pagedDtos.Count < query.PageSize; index++)
            {
                pagedDtos.Add(resultDtos[index]);
            }

            var paginatedResponse = new PaginatedResponse<FeeStudentSearchResultDto>
            {
                Items = pagedDtos,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = totalCount
            };

            var successResponse = CommonResponse<PaginatedResponse<FeeStudentSearchResultDto>>.Success(paginatedResponse);
            return successResponse;
        }

        private static int CompareByOutstandingDescending(FeeStudentSearchResultDto first, FeeStudentSearchResultDto second)
        {
            var outstandingComparison = second.OutstandingAmount.CompareTo(first.OutstandingAmount);
            if (outstandingComparison != 0)
            {
                return outstandingComparison;
            }

            return string.Compare(first.StudentName, second.StudentName, StringComparison.Ordinal);
        }

        public async Task<CommonResponse<FeeAdjustmentDto>> CreateAdjustmentAsync(CreateFeeAdjustmentCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _createAdjustmentValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<FeeAdjustmentDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var enrollment = await _unitOfWork.Enrollments.GetByIdAsync(command.EnrollmentId, cancellationToken);
            if (enrollment == null)
            {
                var notFoundResponse = CommonResponse<FeeAdjustmentDto>.Fail(ResponseCodes.NotFound, "Enrollment with id '" + command.EnrollmentId + "' was not found.");
                return notFoundResponse;
            }

            var typeCode = command.AdjustmentTypeCode.Trim();
            var typeExists = await _unitOfWork.Configs.CodeExistsAsync(ConfigTypeCodes.FeeAdjustmentType, typeCode, cancellationToken);
            if (!typeExists)
            {
                var invalidTypeResponse = CommonResponse<FeeAdjustmentDto>.Fail(ResponseCodes.ValidationError, "AdjustmentTypeCode '" + typeCode + "' is not a known fee adjustment type option.");
                return invalidTypeResponse;
            }

            var categoryCode = command.FeeCategoryCode?.Trim();
            if (!string.IsNullOrWhiteSpace(categoryCode))
            {
                var categoryExists = await _unitOfWork.Configs.CodeExistsAsync(ConfigTypeCodes.FeeCategory, categoryCode, cancellationToken);
                if (!categoryExists)
                {
                    var invalidCategoryResponse = CommonResponse<FeeAdjustmentDto>.Fail(ResponseCodes.ValidationError, "FeeCategoryCode '" + categoryCode + "' is not a known fee category option.");
                    return invalidCategoryResponse;
                }
            }
            else
            {
                categoryCode = null;
            }

            // An adjustment for a month whose invoice is already past Draft can never apply --
            // reject it with a pointer to the alternatives (S1/S2).
            var openInvoices = await _unitOfWork.FeeInvoices.GetStatementByEnrollmentAsync(command.EnrollmentId, cancellationToken);
            foreach (var invoice in openInvoices)
            {
                if (invoice.BillingYear == command.BillingYear
                    && invoice.BillingMonth == command.BillingMonth
                    && invoice.Status != FeeInvoiceStatus.Draft)
                {
                    var lockedMonthResponse = CommonResponse<FeeAdjustmentDto>.Fail(ResponseCodes.Conflict, "This month's invoice is already '" + invoice.Status + "' -- enter the adjustment for a later month or cancel that invoice first.");
                    return lockedMonthResponse;
                }
            }

            var adjustment = new FeeAdjustment
            {
                EnrollmentId = command.EnrollmentId,
                BillingYear = command.BillingYear,
                BillingMonth = command.BillingMonth,
                AdjustmentTypeCode = typeCode,
                FeeCategoryCode = categoryCode,
                Direction = command.Direction,
                ValueType = command.ValueType,
                Value = command.Value,
                Remarks = command.Remarks?.Trim(),
                Status = AdjustmentStatus.Pending
            };

            await _unitOfWork.FeeInvoices.AddAdjustmentAsync(adjustment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var adjustmentDto = await MapAdjustmentWithLabelsAsync(adjustment, cancellationToken);
            var successResponse = CommonResponse<FeeAdjustmentDto>.Success(adjustmentDto, "Fee adjustment recorded; it will apply on the next generation of that month (regenerate the Draft if one already exists).");
            return successResponse;
        }

        // Bulk version of CreateAdjustmentAsync (2026-07-17): one Pending FeeAdjustment per
        // Enrolled enrollment in scope, for occasional one-off charges that hit many students
        // at once for a single billing month (Education Tour, Examination Fee, Training, ...)
        // -- same "created + skipped, with reasons" shape as GenerateAsync, one SaveChangesAsync
        // for the whole batch, and each per-enrollment failure is reported rather than failing
        // the run.
        public async Task<CommonResponse<BulkFeeAdjustmentResultDto>> CreateBulkAdjustmentAsync(CreateBulkFeeAdjustmentCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _createBulkAdjustmentValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<BulkFeeAdjustmentResultDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var academicYear = await _unitOfWork.AcademicYears.GetByIdAsync(command.AcademicYearId, cancellationToken);
            if (academicYear == null)
            {
                var yearNotFoundResponse = CommonResponse<BulkFeeAdjustmentResultDto>.Fail(ResponseCodes.NotFound, "Academic year with id '" + command.AcademicYearId + "' was not found.");
                return yearNotFoundResponse;
            }

            if (command.AcademicClassId.HasValue)
            {
                var academicClass = await _unitOfWork.AcademicClasses.GetByIdAsync(command.AcademicClassId.Value, cancellationToken);
                if (academicClass == null)
                {
                    var classNotFoundResponse = CommonResponse<BulkFeeAdjustmentResultDto>.Fail(ResponseCodes.NotFound, "Academic class with id '" + command.AcademicClassId.Value + "' was not found.");
                    return classNotFoundResponse;
                }

                if (academicClass.AcademicYearId != command.AcademicYearId)
                {
                    var classWrongYearResponse = CommonResponse<BulkFeeAdjustmentResultDto>.Fail(ResponseCodes.ValidationError, "That class does not belong to the given academic year.");
                    return classWrongYearResponse;
                }
            }

            if (command.ClassSectionId.HasValue)
            {
                var section = await _unitOfWork.AcademicClasses.GetSectionByIdAsync(command.ClassSectionId.Value, cancellationToken);
                if (section == null)
                {
                    var sectionNotFoundResponse = CommonResponse<BulkFeeAdjustmentResultDto>.Fail(ResponseCodes.NotFound, "Class section with id '" + command.ClassSectionId.Value + "' was not found.");
                    return sectionNotFoundResponse;
                }

                if (command.AcademicClassId.HasValue && section.AcademicClassId != command.AcademicClassId.Value)
                {
                    var wrongClassResponse = CommonResponse<BulkFeeAdjustmentResultDto>.Fail(ResponseCodes.ValidationError, "That class section does not belong to the given class.");
                    return wrongClassResponse;
                }
            }

            var typeCode = command.AdjustmentTypeCode.Trim();
            var typeExists = await _unitOfWork.Configs.CodeExistsAsync(ConfigTypeCodes.FeeAdjustmentType, typeCode, cancellationToken);
            if (!typeExists)
            {
                var invalidTypeResponse = CommonResponse<BulkFeeAdjustmentResultDto>.Fail(ResponseCodes.ValidationError, "AdjustmentTypeCode '" + typeCode + "' is not a known fee adjustment type option.");
                return invalidTypeResponse;
            }

            var categoryCode = command.FeeCategoryCode?.Trim();
            if (!string.IsNullOrWhiteSpace(categoryCode))
            {
                var categoryExists = await _unitOfWork.Configs.CodeExistsAsync(ConfigTypeCodes.FeeCategory, categoryCode, cancellationToken);
                if (!categoryExists)
                {
                    var invalidCategoryResponse = CommonResponse<BulkFeeAdjustmentResultDto>.Fail(ResponseCodes.ValidationError, "FeeCategoryCode '" + categoryCode + "' is not a known fee category option.");
                    return invalidCategoryResponse;
                }
            }
            else
            {
                categoryCode = null;
            }

            var enrollments = await _unitOfWork.Enrollments.GetEnrolledByYearAsync(command.AcademicYearId, command.AcademicClassId, command.ClassSectionId, cancellationToken);

            var result = new BulkFeeAdjustmentResultDto
            {
                BillingYear = command.BillingYear,
                BillingMonth = command.BillingMonth
            };

            if (enrollments.Count == 0)
            {
                var emptyResponse = CommonResponse<BulkFeeAdjustmentResultDto>.Success(result, "No enrolled students were found for that scope.");
                return emptyResponse;
            }

            var enrollmentIds = new List<Guid>();
            foreach (var enrollment in enrollments)
            {
                enrollmentIds.Add(enrollment.Id);
            }

            // Batched, not per-enrollment (F: the same N+1 avoidance GenerateAsync uses) --
            // every non-cancelled invoice these enrollments already have, so a month that's
            // already past Draft for a given student can be skipped with a reason instead of
            // 500ing on the unique index.
            var existingInvoices = await _unitOfWork.FeeInvoices.GetByEnrollmentIdsAsync(enrollmentIds, cancellationToken);
            var lockedEnrollmentIds = new List<Guid>();
            foreach (var invoice in existingInvoices)
            {
                if (invoice.BillingYear == command.BillingYear
                    && invoice.BillingMonth == command.BillingMonth
                    && invoice.Status != FeeInvoiceStatus.Draft)
                {
                    lockedEnrollmentIds.Add(invoice.EnrollmentId);
                }
            }

            foreach (var enrollment in enrollments)
            {
                var studentName = BuildStudentName(enrollment.Student);

                if (lockedEnrollmentIds.Contains(enrollment.Id))
                {
                    result.Skipped.Add(new FeeGenerationSkipDto
                    {
                        EnrollmentId = enrollment.Id,
                        StudentName = studentName,
                        Reason = "This month's invoice is already generated for this student -- enter the adjustment for a later month or cancel that invoice first."
                    });
                    continue;
                }

                var adjustment = new FeeAdjustment
                {
                    EnrollmentId = enrollment.Id,
                    BillingYear = command.BillingYear,
                    BillingMonth = command.BillingMonth,
                    AdjustmentTypeCode = typeCode,
                    FeeCategoryCode = categoryCode,
                    Direction = command.Direction,
                    ValueType = command.ValueType,
                    Value = command.Value,
                    Remarks = command.Remarks?.Trim(),
                    Status = AdjustmentStatus.Pending
                };

                await _unitOfWork.FeeInvoices.AddAdjustmentAsync(adjustment, cancellationToken);
                result.CreatedAdjustmentIds.Add(adjustment.Id);
                result.CreatedCount++;
            }

            result.SkippedCount = result.Skipped.Count;

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var successResponse = CommonResponse<BulkFeeAdjustmentResultDto>.Success(result, "Created " + result.CreatedCount + " adjustment(s); skipped " + result.SkippedCount + ".");
            return successResponse;
        }

        public async Task<CommonResponse<List<FeeAdjustmentDto>>> GetAdjustmentsAsync(GetFeeAdjustmentsQuery query, CancellationToken cancellationToken = default)
        {
            var adjustments = await _unitOfWork.FeeInvoices.GetAdjustmentsByFilterAsync(query.EnrollmentId, query.BillingYear, query.BillingMonth, query.Status, cancellationToken);
            var feeLabels = await LoadFeeLabelMapAsync(cancellationToken);

            var adjustmentDtos = new List<FeeAdjustmentDto>();
            foreach (var adjustment in adjustments)
            {
                var adjustmentDto = FeeInvoiceMapper.ToAdjustmentDto(adjustment, feeLabels);
                adjustmentDtos.Add(adjustmentDto);
            }

            var successResponse = CommonResponse<List<FeeAdjustmentDto>>.Success(adjustmentDtos);
            return successResponse;
        }

        public async Task<CommonResponse<FeeAdjustmentDto>> UpdateAdjustmentAsync(Guid adjustmentId, UpdateFeeAdjustmentCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _updateAdjustmentValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<FeeAdjustmentDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var adjustment = await _unitOfWork.FeeInvoices.GetAdjustmentByIdAsync(adjustmentId, cancellationToken);
            if (adjustment == null)
            {
                var notFoundResponse = CommonResponse<FeeAdjustmentDto>.Fail(ResponseCodes.NotFound, "Fee adjustment with id '" + adjustmentId + "' was not found.");
                return notFoundResponse;
            }

            if (adjustment.Status != AdjustmentStatus.Pending)
            {
                var lockedResponse = CommonResponse<FeeAdjustmentDto>.Fail(ResponseCodes.Conflict, "Only Pending adjustments can be edited; this one is '" + adjustment.Status + "'.");
                return lockedResponse;
            }

            var typeCode = command.AdjustmentTypeCode.Trim();
            var typeExists = await _unitOfWork.Configs.CodeExistsAsync(ConfigTypeCodes.FeeAdjustmentType, typeCode, cancellationToken);
            if (!typeExists)
            {
                var invalidTypeResponse = CommonResponse<FeeAdjustmentDto>.Fail(ResponseCodes.ValidationError, "AdjustmentTypeCode '" + typeCode + "' is not a known fee adjustment type option.");
                return invalidTypeResponse;
            }

            var categoryCode = command.FeeCategoryCode?.Trim();
            if (!string.IsNullOrWhiteSpace(categoryCode))
            {
                var categoryExists = await _unitOfWork.Configs.CodeExistsAsync(ConfigTypeCodes.FeeCategory, categoryCode, cancellationToken);
                if (!categoryExists)
                {
                    var invalidCategoryResponse = CommonResponse<FeeAdjustmentDto>.Fail(ResponseCodes.ValidationError, "FeeCategoryCode '" + categoryCode + "' is not a known fee category option.");
                    return invalidCategoryResponse;
                }
            }
            else
            {
                categoryCode = null;
            }

            adjustment.AdjustmentTypeCode = typeCode;
            adjustment.FeeCategoryCode = categoryCode;
            adjustment.Direction = command.Direction;
            adjustment.ValueType = command.ValueType;
            adjustment.Value = command.Value;
            adjustment.Remarks = command.Remarks?.Trim();

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var adjustmentDto = await MapAdjustmentWithLabelsAsync(adjustment, cancellationToken);
            var successResponse = CommonResponse<FeeAdjustmentDto>.Success(adjustmentDto, "Fee adjustment updated successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<bool>> CancelAdjustmentAsync(Guid adjustmentId, CancellationToken cancellationToken = default)
        {
            var adjustment = await _unitOfWork.FeeInvoices.GetAdjustmentByIdAsync(adjustmentId, cancellationToken);
            if (adjustment == null)
            {
                var notFoundResponse = CommonResponse<bool>.Fail(ResponseCodes.NotFound, "Fee adjustment with id '" + adjustmentId + "' was not found.");
                return notFoundResponse;
            }

            if (adjustment.Status != AdjustmentStatus.Pending)
            {
                var lockedResponse = CommonResponse<bool>.Fail(ResponseCodes.Conflict, "Only Pending adjustments can be cancelled; this one is '" + adjustment.Status + "'.");
                return lockedResponse;
            }

            adjustment.Status = AdjustmentStatus.Cancelled;
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var successResponse = CommonResponse<bool>.Success(true, "Fee adjustment cancelled.");
            return successResponse;
        }

        private async Task<Dictionary<Guid, List<StudentDiscount>>> LoadDiscountsByEnrollmentAsync(IReadOnlyList<Guid> enrollmentIds, CancellationToken cancellationToken)
        {
            var discounts = await _unitOfWork.Enrollments.GetDiscountsByEnrollmentIdsAsync(enrollmentIds, cancellationToken);

            var discountsByEnrollment = new Dictionary<Guid, List<StudentDiscount>>();
            foreach (var discount in discounts)
            {
                if (!discountsByEnrollment.TryGetValue(discount.EnrollmentId, out var enrollmentDiscounts))
                {
                    enrollmentDiscounts = new List<StudentDiscount>();
                    discountsByEnrollment[discount.EnrollmentId] = enrollmentDiscounts;
                }

                enrollmentDiscounts.Add(discount);
            }

            return discountsByEnrollment;
        }

        private async Task<Dictionary<Guid, List<StudentScholarship>>> LoadScholarshipsByEnrollmentAsync(IReadOnlyList<Guid> enrollmentIds, CancellationToken cancellationToken)
        {
            var scholarships = await _unitOfWork.Enrollments.GetScholarshipsByEnrollmentIdsAsync(enrollmentIds, cancellationToken);

            var scholarshipsByEnrollment = new Dictionary<Guid, List<StudentScholarship>>();
            foreach (var scholarship in scholarships)
            {
                if (!scholarshipsByEnrollment.TryGetValue(scholarship.EnrollmentId, out var enrollmentScholarships))
                {
                    enrollmentScholarships = new List<StudentScholarship>();
                    scholarshipsByEnrollment[scholarship.EnrollmentId] = enrollmentScholarships;
                }

                enrollmentScholarships.Add(scholarship);
            }

            return scholarshipsByEnrollment;
        }

        private async Task<Dictionary<Guid, List<Guid>>> LoadFeeSelectionsByEnrollmentAsync(IReadOnlyList<Guid> enrollmentIds, CancellationToken cancellationToken)
        {
            var feeSelections = await _unitOfWork.Enrollments.GetFeeSelectionsByEnrollmentIdsAsync(enrollmentIds, cancellationToken);

            var selectionsByEnrollment = new Dictionary<Guid, List<Guid>>();
            foreach (var feeSelection in feeSelections)
            {
                if (!selectionsByEnrollment.TryGetValue(feeSelection.EnrollmentId, out var selectedItemIds))
                {
                    selectedItemIds = new List<Guid>();
                    selectionsByEnrollment[feeSelection.EnrollmentId] = selectedItemIds;
                }

                selectedItemIds.Add(feeSelection.FeeStructureItemId);
            }

            return selectionsByEnrollment;
        }

        // Merged label map for every catalog a fee-invoice line's code can come from -- fee
        // categories plus the discount/scholarship/adjustment type catalogs (their code
        // namespaces are distinct by convention). Used both when composing new line
        // Descriptions (FeeInvoiceFactory) and when mapping read DTOs.
        private async Task<Dictionary<string, string>> LoadFeeLabelMapAsync(CancellationToken cancellationToken)
        {
            var labelsByCode = ConfigLabelHelper.BuildLabelMap(await _unitOfWork.Configs.GetByTypeCodeAsync(ConfigTypeCodes.FeeCategory, cancellationToken));
            ConfigLabelHelper.MergeLabelMap(labelsByCode, await _unitOfWork.Configs.GetByTypeCodeAsync(ConfigTypeCodes.DiscountType, cancellationToken));
            ConfigLabelHelper.MergeLabelMap(labelsByCode, await _unitOfWork.Configs.GetByTypeCodeAsync(ConfigTypeCodes.ScholarshipType, cancellationToken));
            ConfigLabelHelper.MergeLabelMap(labelsByCode, await _unitOfWork.Configs.GetByTypeCodeAsync(ConfigTypeCodes.FeeAdjustmentType, cancellationToken));
            return labelsByCode;
        }

        private async Task<FeeInvoiceDto> MapInvoiceWithLabelsAsync(FeeInvoice invoice, CancellationToken cancellationToken)
        {
            var feeLabels = await LoadFeeLabelMapAsync(cancellationToken);
            var invoiceDto = FeeInvoiceMapper.ToDto(invoice, includeLines: true, feeLabels);
            return invoiceDto;
        }

        private async Task<FeeAdjustmentDto> MapAdjustmentWithLabelsAsync(FeeAdjustment adjustment, CancellationToken cancellationToken)
        {
            var feeLabels = await LoadFeeLabelMapAsync(cancellationToken);
            var adjustmentDto = FeeInvoiceMapper.ToAdjustmentDto(adjustment, feeLabels);
            return adjustmentDto;
        }

        private async Task<DateTime> ResolveDueDateAsync(int billingYear, int billingMonth, CancellationToken cancellationToken)
        {
            var dueDay = DefaultDueDay;

            var dueDayConfig = await _unitOfWork.AppConfigs.GetByConfigParamAsync(DueDayConfigParam, cancellationToken);
            if (dueDayConfig != null && int.TryParse(dueDayConfig.ConfigValue, out var configuredDay) && configuredDay >= 1 && configuredDay <= 31)
            {
                dueDay = configuredDay;
            }

            var dueDate = FeeInvoiceFactory.ResolveDueDate(billingYear, billingMonth, dueDay);
            return dueDate;
        }

        // Pending = Generated + past due + balance remaining (F6). Returns whether anything
        // changed so the caller can persist the stamp.
        private static bool RefreshDerivedStatuses(IReadOnlyList<FeeInvoice> invoices)
        {
            var today = DateTime.UtcNow.Date;
            var anyChanged = false;

            foreach (var invoice in invoices)
            {
                if (invoice.Status == FeeInvoiceStatus.Generated
                    && invoice.DueDate.Date < today
                    && invoice.NetAmount - invoice.PaidAmount > 0m)
                {
                    invoice.Status = FeeInvoiceStatus.Pending;
                    anyChanged = true;
                }
            }

            return anyChanged;
        }

        // Same-day entries keep invoices ahead of payments so the running balance never dips
        // negative just because a payment was recorded the morning its invoice was finalized.
        // Rank-based (not a binary "Invoice or not" check) so it stays correct if a third entry
        // type is ever added -- a once-real CarryForward entry type was removed when carrying a
        // balance forward switched to voiding the source invoice outright (excluded from this
        // query's `invoices` entirely) instead of a same-invoice offsetting ledger line.
        private static int CompareAccountEntries(FeeAccountEntryDto first, FeeAccountEntryDto second)
        {
            var dateComparison = first.Date.Date.CompareTo(second.Date.Date);
            if (dateComparison != 0)
            {
                return dateComparison;
            }

            var rankComparison = GetEntryTypeRank(first.EntryType).CompareTo(GetEntryTypeRank(second.EntryType));
            if (rankComparison != 0)
            {
                return rankComparison;
            }

            return string.Compare(first.Reference, second.Reference, StringComparison.Ordinal);
        }

        private static int GetEntryTypeRank(string entryType)
        {
            if (entryType == "Invoice")
            {
                return 0;
            }

            if (entryType == "Payment")
            {
                return 2;
            }

            // Everything else is a reduction line (Discount/Scholarship/Rule Discount/
            // Adjustment) -- ranked between its invoice and the payment rows so it reads as
            // "invoice, then what was knocked off it, then what was paid".
            return 1;
        }

        private static string GetReductionEntryType(FeeLineSource source)
        {
            if (source == FeeLineSource.Discount)
            {
                return "Discount";
            }

            if (source == FeeLineSource.Scholarship)
            {
                return "Scholarship";
            }

            if (source == FeeLineSource.RuleDiscount)
            {
                return "Rule Discount";
            }

            return "Adjustment";
        }

        private static string BuildStudentName(Student student)
        {
            if (student == null)
            {
                return null;
            }

            var nameParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(student.FirstName))
            {
                nameParts.Add(student.FirstName);
            }

            if (!string.IsNullOrWhiteSpace(student.MiddleName))
            {
                nameParts.Add(student.MiddleName);
            }

            if (!string.IsNullOrWhiteSpace(student.LastName))
            {
                nameParts.Add(student.LastName);
            }

            var fullName = string.Join(" ", nameParts);
            return fullName;
        }

        private static string BuildValidationErrorMessage(ValidationResult validationResult)
        {
            var errorMessages = new List<string>();
            foreach (var failure in validationResult.Errors)
            {
                errorMessages.Add(failure.ErrorMessage);
            }

            var combinedMessage = string.Join(" ", errorMessages);
            return combinedMessage;
        }
    }
}
