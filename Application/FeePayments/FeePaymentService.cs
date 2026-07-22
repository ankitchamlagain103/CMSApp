using Application.Common.Helpers;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.DocumentTemplates;
using Application.FeeInvoices;
using Application.FeePayments.Commands;
using Application.FeePayments.Dtos;
using Application.FeePayments.Queries;
using Application.FeePayments.Validators;
using Application.FeeRules.Evaluation;
using Domain.Common.Filters;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using FluentValidation.Results;

namespace Application.FeePayments
{
    public class FeePaymentService : IFeePaymentService
    {
        private const string ReceiptNoPrefix = "RCP";
        private const string DueDayConfigParam = "FEE_DUE_DAY_OF_MONTH";
        private const int DefaultDueDay = 10;

        // Sanity bound on one payment's advance-billing extension -- a typo'd amount (or a
        // dangling percentage rule) must not be able to bill a family a decade ahead.
        private const int MaxAdvanceBillingMonths = 12;

        private readonly IUnitOfWork _unitOfWork;
        private readonly CreateFeePaymentCommandValidator _createValidator;

        public FeePaymentService(
            IUnitOfWork unitOfWork,
            CreateFeePaymentCommandValidator createValidator)
        {
            _unitOfWork = unitOfWork;
            _createValidator = createValidator;
        }

        public async Task<CommonResponse<FeePaymentPreviewDto>> PreviewAsync(CreateFeePaymentCommand command, CancellationToken cancellationToken = default)
        {
            var planResult = await BuildPlanAsync(command, cancellationToken);
            if (planResult.ErrorCode != null)
            {
                var failureResponse = CommonResponse<FeePaymentPreviewDto>.Fail(planResult.ErrorCode, planResult.ErrorMessage);
                return failureResponse;
            }

            var feeLabels = await LoadFeeLabelMapAsync(cancellationToken);
            var previewDto = BuildPreviewDto(command, planResult, feeLabels);

            var message = previewDto.UnallocatedAmount > 0m
                ? "After rule discounts, only " + (command.Amount - previewDto.UnallocatedAmount).ToString("F2") + " is owed -- collect that amount instead."
                : "Payment plan computed.";

            var successResponse = CommonResponse<FeePaymentPreviewDto>.Success(previewDto, message);
            return successResponse;
        }

        public async Task<CommonResponse<FeePaymentDto>> CreateAsync(CreateFeePaymentCommand command, CancellationToken cancellationToken = default)
        {
            var planResult = await BuildPlanAsync(command, cancellationToken);
            if (planResult.ErrorCode != null)
            {
                var failureResponse = CommonResponse<FeePaymentDto>.Fail(planResult.ErrorCode, planResult.ErrorMessage);
                return failureResponse;
            }

            if (planResult.UnallocatedAmount > 0m)
            {
                var collectable = command.Amount - planResult.UnallocatedAmount;
                var overpaymentResponse = CommonResponse<FeePaymentDto>.Fail(ResponseCodes.ValidationError, "After rule discounts, only " + collectable.ToString("F2") + " is owed -- collect exactly that amount (over-payment is not supported).");
                return overpaymentResponse;
            }

            var receiptPrefix = ReceiptNoPrefix + command.PaymentDate.Year;
            var existingReceiptNos = await _unitOfWork.FeeInvoices.GetReceiptNosByPrefixAsync(receiptPrefix, cancellationToken);
            var receiptNo = NumberSequenceHelper.Next(receiptPrefix, existingReceiptNos, 4);

            var payment = new FeePayment
            {
                Id = Guid.NewGuid(),
                ReceiptNo = receiptNo,
                EnrollmentId = command.EnrollmentId,
                PaymentDate = DateTimeHelper.AsUtcDate(command.PaymentDate),
                Amount = command.Amount,
                PaymentMode = command.PaymentMode,
                ReferenceNo = command.ReferenceNo?.Trim(),
                Status = FeePaymentStatus.Confirmed,
                Remarks = command.Remarks?.Trim(),
                Enrollment = planResult.Enrollment
            };

            // Advance-billed invoices (2026-07-17) are brand-new entities -- AddAsync cascades
            // to their already-built Lines (same convention as FeeInvoiceService.GenerateAsync).
            foreach (var newInvoice in planResult.NewlyCreatedInvoices)
            {
                await _unitOfWork.FeeInvoices.AddAsync(newInvoice, cancellationToken);
            }

            // Rule discounts append as machine-generated RuleDiscount lines -- the single
            // sanctioned post-finalization invoice mutation (F5) -- and the invoice totals are
            // re-derived from its lines.
            foreach (var ruleDiscount in planResult.RuleDiscounts)
            {
                var invoice = planResult.InvoicesById[ruleDiscount.FeeInvoiceId];

                var discountLine = new FeeInvoiceLine
                {
                    FeeInvoiceId = invoice.Id,
                    Source = FeeLineSource.RuleDiscount,
                    FeeRuleId = ruleDiscount.FeeRuleId,
                    Description = ruleDiscount.Description,
                    Amount = -ruleDiscount.Amount,
                    FeeInvoice = invoice
                };

                invoice.Lines.Add(discountLine);
                await _unitOfWork.FeeInvoices.AddLineAsync(discountLine, cancellationToken);

                FeeInvoiceFactory.RecomputeTotals(invoice);
            }

            foreach (var plannedAllocation in planResult.Allocations)
            {
                var invoice = planResult.InvoicesById[plannedAllocation.FeeInvoiceId];

                var allocation = new FeePaymentAllocation
                {
                    FeePaymentId = payment.Id,
                    FeeInvoiceId = invoice.Id,
                    Amount = plannedAllocation.Amount,
                    FeePayment = payment,
                    FeeInvoice = invoice
                };

                payment.Allocations.Add(allocation);

                invoice.PaidAmount += plannedAllocation.Amount;
                RederivePaymentStatus(invoice);
            }

            await _unitOfWork.FeeInvoices.AddPaymentAsync(payment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var paymentDto = FeePaymentMapper.ToDto(payment);
            paymentDto.MonthsBilledInAdvance = planResult.NewlyCreatedInvoices.Count;

            var newlyCreatedInvoiceIds = new List<Guid>();
            foreach (var newInvoice in planResult.NewlyCreatedInvoices)
            {
                newlyCreatedInvoiceIds.Add(newInvoice.Id);
            }

            foreach (var allocationDto in paymentDto.Allocations)
            {
                allocationDto.IsNewlyGenerated = newlyCreatedInvoiceIds.Contains(allocationDto.FeeInvoiceId);
            }

            var successMessage = "Payment recorded; receipt " + receiptNo + "."
                + (paymentDto.MonthsBilledInAdvance > 0 ? " Billed " + paymentDto.MonthsBilledInAdvance + " month(s) in advance." : string.Empty);
            var successResponse = CommonResponse<FeePaymentDto>.Success(paymentDto, successMessage);
            return successResponse;
        }

        public async Task<CommonResponse<PaginatedResponse<FeePaymentDto>>> GetPaymentsAsync(GetFeePaymentsQuery query, CancellationToken cancellationToken = default)
        {
            var filter = new FeePaymentFilter
            {
                EnrollmentId = query.EnrollmentId,
                FromDate = DateTimeHelper.AsUtcDate(query.FromDate),
                ToDate = DateTimeHelper.AsUtcDate(query.ToDate),
                PaymentMode = query.PaymentMode,
                Status = query.Status,
                Search = query.Search
            };

            var pagedPayments = await _unitOfWork.FeeInvoices.GetPagedPaymentsByFilterAsync(filter, query.Page, query.PageSize, cancellationToken);

            var paymentDtos = new List<FeePaymentDto>();
            foreach (var payment in pagedPayments.Items)
            {
                var paymentDto = FeePaymentMapper.ToDto(payment);
                paymentDtos.Add(paymentDto);
            }

            var paginatedResponse = new PaginatedResponse<FeePaymentDto>
            {
                Items = paymentDtos,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = pagedPayments.TotalCount
            };

            var successResponse = CommonResponse<PaginatedResponse<FeePaymentDto>>.Success(paginatedResponse);
            return successResponse;
        }

        public async Task<CommonResponse<FeePaymentDto>> GetPaymentByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var payment = await _unitOfWork.FeeInvoices.GetPaymentByIdAsync(id, cancellationToken);
            if (payment == null)
            {
                var notFoundResponse = CommonResponse<FeePaymentDto>.Fail(ResponseCodes.NotFound, "Fee payment with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            var paymentDto = FeePaymentMapper.ToDto(payment);
            var successResponse = CommonResponse<FeePaymentDto>.Success(paymentDto);
            return successResponse;
        }

        // Payments are append-only (F10): a mistake is voided -- allocations reversed, invoice
        // statuses re-derived -- and re-entered. RuleDiscount lines earned at confirm time are
        // deliberately NOT reverted here: a corrected re-entry normally re-earns them, and
        // silently un-discounting an invoice would be more surprising than a rare manual
        // cleanup of a stray discount line.
        public async Task<CommonResponse<FeePaymentDto>> VoidAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var payment = await _unitOfWork.FeeInvoices.GetPaymentByIdAsync(id, cancellationToken);
            if (payment == null)
            {
                var notFoundResponse = CommonResponse<FeePaymentDto>.Fail(ResponseCodes.NotFound, "Fee payment with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            if (payment.Status != FeePaymentStatus.Confirmed)
            {
                var alreadyVoidedResponse = CommonResponse<FeePaymentDto>.Fail(ResponseCodes.Conflict, "This payment is already voided.");
                return alreadyVoidedResponse;
            }

            var allocationsToRemove = new List<FeePaymentAllocation>(payment.Allocations);
            foreach (var allocation in allocationsToRemove)
            {
                var invoice = allocation.FeeInvoice;
                if (invoice != null)
                {
                    invoice.PaidAmount = Math.Max(0m, invoice.PaidAmount - allocation.Amount);
                    RederivePaymentStatus(invoice);
                }

                payment.Allocations.Remove(allocation);
                _unitOfWork.FeeInvoices.RemoveAllocation(allocation);
            }

            payment.Status = FeePaymentStatus.Voided;

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var paymentDto = FeePaymentMapper.ToDto(payment);
            var successResponse = CommonResponse<FeePaymentDto>.Success(paymentDto, "Payment voided; its allocations were reversed.");
            return successResponse;
        }

        // Renders the admin-configurable PaymentReceipt template into printable HTML: the
        // payment header, one row per allocated invoice, and every line of those invoices (the
        // "print receipt should include the invoice line details" requirement), plus the
        // enrollment's live outstanding balance after this payment.
        public async Task<CommonResponse<DocumentPreviewDto>> GetReceiptAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var payment = await _unitOfWork.FeeInvoices.GetPaymentByIdAsync(id, cancellationToken);
            if (payment == null)
            {
                var notFoundResponse = CommonResponse<DocumentPreviewDto>.Fail(ResponseCodes.NotFound, "Fee payment with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            if (payment.Status == FeePaymentStatus.Voided)
            {
                var voidedResponse = CommonResponse<DocumentPreviewDto>.Fail(ResponseCodes.Conflict, "This payment is voided -- a receipt cannot be printed for it.");
                return voidedResponse;
            }

            var documentTemplate = await _unitOfWork.DocumentTemplates.GetByTemplateTypeAsync(DocumentTemplateType.PaymentReceipt, cancellationToken);
            if (documentTemplate == null)
            {
                var noTemplateResponse = CommonResponse<DocumentPreviewDto>.Fail(ResponseCodes.NotFound, "No document template is configured for '" + DocumentTemplateType.PaymentReceipt + "' yet.");
                return noTemplateResponse;
            }

            var enrollment = await _unitOfWork.Enrollments.GetWithDetailsAsync(payment.EnrollmentId, cancellationToken);

            var allocatedInvoiceIds = new List<Guid>();
            foreach (var allocation in payment.Allocations)
            {
                allocatedInvoiceIds.Add(allocation.FeeInvoiceId);
            }

            var allocatedInvoices = allocatedInvoiceIds.Count > 0
                ? await _unitOfWork.FeeInvoices.GetByIdsWithLinesAsync(allocatedInvoiceIds, cancellationToken)
                : new List<FeeInvoice>();

            // Live outstanding across the enrollment's non-draft invoices (F7) -- payments
            // already made (this one included) are reflected in each invoice's PaidAmount.
            var statementInvoices = await _unitOfWork.FeeInvoices.GetStatementByEnrollmentAsync(payment.EnrollmentId, cancellationToken);
            decimal outstandingAmount = 0m;
            foreach (var statementInvoice in statementInvoices)
            {
                if (statementInvoice.Status != FeeInvoiceStatus.Draft)
                {
                    outstandingAmount += statementInvoice.NetAmount - statementInvoice.PaidAmount;
                }
            }

            var studentName = string.Empty;
            var admissionNo = string.Empty;
            var gradeCode = string.Empty;
            var sectionCode = string.Empty;
            if (enrollment != null)
            {
                if (enrollment.Student != null)
                {
                    var nameParts = new List<string>();
                    if (!string.IsNullOrWhiteSpace(enrollment.Student.FirstName))
                    {
                        nameParts.Add(enrollment.Student.FirstName);
                    }

                    if (!string.IsNullOrWhiteSpace(enrollment.Student.MiddleName))
                    {
                        nameParts.Add(enrollment.Student.MiddleName);
                    }

                    if (!string.IsNullOrWhiteSpace(enrollment.Student.LastName))
                    {
                        nameParts.Add(enrollment.Student.LastName);
                    }

                    studentName = string.Join(" ", nameParts);
                    admissionNo = enrollment.Student.AdmissionNo;
                }

                if (enrollment.ClassSection != null)
                {
                    sectionCode = enrollment.ClassSection.SectionCode;
                    gradeCode = enrollment.ClassSection.AcademicClass?.GradeCode;
                }
            }

            var schoolName = await GetAppConfigValueAsync("APP_NAME", cancellationToken);
            var schoolAddress = await GetAppConfigValueAsync("SCHOOL_ADDRESS", cancellationToken);
            var schoolPhone = await GetAppConfigValueAsync("SCHOOL_PHONE", cancellationToken);

            var placeholderValues = new Dictionary<string, string>
            {
                { "SchoolName", schoolName },
                { "SchoolAddress", schoolAddress },
                { "SchoolPhone", schoolPhone },
                { "ReceiptNo", payment.ReceiptNo },
                { "PaymentDate", payment.PaymentDate.ToString("yyyy-MM-dd") },
                { "StudentName", studentName },
                { "AdmissionNo", admissionNo },
                { "GradeCode", gradeCode },
                { "SectionCode", sectionCode },
                { "PaymentMode", payment.PaymentMode.ToString() },
                { "ReferenceNo", string.IsNullOrWhiteSpace(payment.ReferenceNo) ? string.Empty : "(Ref " + payment.ReferenceNo + ")" },
                { "AmountPaid", payment.Amount.ToString("F2") },
                { "Remarks", payment.Remarks },
                { "AllocationsRows", BuildAllocationsRows(payment) },
                { "InvoiceLinesRows", BuildInvoiceLinesRows(allocatedInvoices) },
                { "OutstandingAmount", outstandingAmount.ToString("F2") }
            };

            var renderedHtml = TemplateRenderer.Render(documentTemplate.HtmlContent, placeholderValues);

            var documentPreviewDto = new DocumentPreviewDto
            {
                TemplateType = DocumentTemplateType.PaymentReceipt,
                Html = renderedHtml
            };

            var receiptSuccessResponse = CommonResponse<DocumentPreviewDto>.Success(documentPreviewDto);
            return receiptSuccessResponse;
        }

        // "How much for X months?" -- the answer to the Collect Fee Payment form's "how do I
        // know the amount" gap (2026-07-17). Purely read-only: any additional months it needs
        // to project are built the same way BuildAdvanceInvoicesAsync builds them for a real
        // payment, but never AddAsync'd/SaveChanges'd -- plain in-memory objects the caller
        // discards after reading their NetAmount.
        public async Task<CommonResponse<FeeAdvanceQuoteDto>> GetAdvanceQuoteAsync(GetFeeAdvanceQuoteQuery query, CancellationToken cancellationToken = default)
        {
            if (query.MonthsToPay < 1)
            {
                var invalidMonthsResponse = CommonResponse<FeeAdvanceQuoteDto>.Fail(ResponseCodes.ValidationError, "MonthsToPay must be at least 1.");
                return invalidMonthsResponse;
            }

            var enrollment = await _unitOfWork.Enrollments.GetWithDetailsAsync(query.EnrollmentId, cancellationToken);
            if (enrollment == null)
            {
                var notFoundResponse = CommonResponse<FeeAdvanceQuoteDto>.Fail(ResponseCodes.NotFound, "Enrollment with id '" + query.EnrollmentId + "' was not found.");
                return notFoundResponse;
            }

            var paymentDate = DateTimeHelper.AsUtcDate(query.PaymentDate ?? DateTime.UtcNow);

            var openInvoices = await _unitOfWork.FeeInvoices.GetOpenByEnrollmentAsync(query.EnrollmentId, cancellationToken);

            var settledInvoices = new List<FeeInvoice>(openInvoices);
            var additionalMonthsNeeded = Math.Max(0, query.MonthsToPay - openInvoices.Count);

            if (additionalMonthsNeeded > 0)
            {
                var advanceResult = await BuildAdvanceInvoicesAsync(enrollment, null, additionalMonthsNeeded, paymentDate, cancellationToken);
                if (advanceResult.ErrorCode != null)
                {
                    var advanceFailureResponse = CommonResponse<FeeAdvanceQuoteDto>.Fail(advanceResult.ErrorCode, advanceResult.ErrorMessage);
                    return advanceFailureResponse;
                }

                settledInvoices.AddRange(advanceResult.Invoices);
            }

            // openInvoices is chronologically ordered (GetOpenByEnrollmentAsync) and the newly
            // built months continue strictly after them, so Take() here is already the earliest
            // MonthsToPay periods -- exactly what a real payment would settle first (FIFO).
            var monthsToQuote = new List<FeeInvoice>();
            var takeCount = Math.Min(query.MonthsToPay, settledInvoices.Count);
            for (var i = 0; i < takeCount; i++)
            {
                monthsToQuote.Add(settledInvoices[i]);
            }

            var openInvoiceIds = new List<Guid>();
            foreach (var openInvoice in openInvoices)
            {
                openInvoiceIds.Add(openInvoice.Id);
            }

            var newlyGeneratedIds = new List<Guid>();
            foreach (var invoice in settledInvoices)
            {
                if (!openInvoiceIds.Contains(invoice.Id))
                {
                    newlyGeneratedIds.Add(invoice.Id);
                }
            }

            decimal grossAmount = 0m;
            var monthDtos = new List<FeeAdvanceQuoteMonthDto>();
            foreach (var invoice in monthsToQuote)
            {
                var balance = invoice.NetAmount - invoice.PaidAmount;
                grossAmount += balance;

                var monthDto = new FeeAdvanceQuoteMonthDto
                {
                    BillingYear = invoice.BillingYear,
                    BillingMonth = invoice.BillingMonth,
                    NetAmount = balance,
                    IsAlreadyGenerated = !newlyGeneratedIds.Contains(invoice.Id)
                };
                monthDtos.Add(monthDto);
            }

            var ruleDiscountDtos = new List<FeeRuleDiscountDto>();
            decimal ruleDiscountTotal = 0m;

            if (query.ApplyRuleDiscounts && monthsToQuote.Count > 0)
            {
                var activeRules = await _unitOfWork.FeeRules.GetActiveRulesAsync(FeeRuleTrigger.OnPayment, paymentDate, cancellationToken);
                if (activeRules.Count > 0)
                {
                    var context = new FeeRuleContext
                    {
                        EnrollmentId = enrollment.Id,
                        AcademicClassId = enrollment.ClassSection.AcademicClassId,
                        PaymentDate = paymentDate
                    };

                    foreach (var invoice in monthsToQuote)
                    {
                        var invoiceContext = BuildInvoiceContext(invoice, invoice.NetAmount - invoice.PaidAmount);
                        context.FullySettledInvoices.Add(invoiceContext);
                    }

                    var ruleDiscounts = FeeRuleEngine.Evaluate(activeRules, context);
                    foreach (var ruleDiscount in ruleDiscounts)
                    {
                        // Same per-invoice balance cap as the real payment flow -- a stacked
                        // percentage can't discount past what that month actually owes.
                        FeeInvoice matchingInvoice = null;
                        foreach (var invoice in monthsToQuote)
                        {
                            if (invoice.Id == ruleDiscount.FeeInvoiceId)
                            {
                                matchingInvoice = invoice;
                            }
                        }

                        var invoiceBalance = matchingInvoice != null ? matchingInvoice.NetAmount - matchingInvoice.PaidAmount : 0m;
                        var cappedAmount = Math.Min(ruleDiscount.Amount, invoiceBalance);
                        if (cappedAmount <= 0m)
                        {
                            continue;
                        }

                        ruleDiscountTotal += cappedAmount;

                        var ruleDiscountDto = new FeeRuleDiscountDto
                        {
                            FeeRuleId = ruleDiscount.FeeRuleId,
                            RuleCode = ruleDiscount.RuleCode,
                            RuleName = ruleDiscount.RuleName,
                            FeeInvoiceId = ruleDiscount.FeeInvoiceId,
                            Amount = cappedAmount,
                            Description = ruleDiscount.Description
                        };
                        ruleDiscountDtos.Add(ruleDiscountDto);
                    }
                }
            }

            var quoteDto = new FeeAdvanceQuoteDto
            {
                EnrollmentId = query.EnrollmentId,
                MonthsRequested = query.MonthsToPay,
                MonthsAvailable = monthsToQuote.Count,
                GrossAmount = grossAmount,
                RuleDiscountTotal = ruleDiscountTotal,
                NetAmountToCollect = grossAmount - ruleDiscountTotal,
                Months = monthDtos,
                RuleDiscounts = ruleDiscountDtos
            };

            var successResponse = CommonResponse<FeeAdvanceQuoteDto>.Success(quoteDto);
            return successResponse;
        }

        private async Task<string> GetAppConfigValueAsync(string configParam, CancellationToken cancellationToken)
        {
            var appConfig = await _unitOfWork.AppConfigs.GetByConfigParamAsync(configParam, cancellationToken);
            return appConfig?.ConfigValue ?? string.Empty;
        }

        private static string BuildAllocationsRows(FeePayment payment)
        {
            var rowsHtml = string.Empty;
            foreach (var allocation in payment.Allocations)
            {
                var invoiceNo = allocation.FeeInvoice?.InvoiceNo;
                var billingMonthLabel = allocation.FeeInvoice != null
                    ? new DateTime(allocation.FeeInvoice.BillingYear, allocation.FeeInvoice.BillingMonth, 1).ToString("MMMM yyyy")
                    : string.Empty;
                rowsHtml += "<tr><td>" + invoiceNo + "</td><td>" + billingMonthLabel + "</td><td>" + allocation.Amount.ToString("F2") + "</td></tr>";
            }

            return rowsHtml;
        }

        // Sr.No-led, matching the "Receipt Sample for Fees" layout's Particulars table.
        private static string BuildInvoiceLinesRows(IReadOnlyList<FeeInvoice> allocatedInvoices)
        {
            var rowsHtml = string.Empty;
            var serialNumber = 1;
            foreach (var invoice in allocatedInvoices)
            {
                foreach (var line in invoice.Lines)
                {
                    rowsHtml += "<tr><td>" + serialNumber + "</td><td>" + invoice.InvoiceNo + "</td><td>" + line.Description + "</td><td>" + line.Amount.ToString("F2") + "</td></tr>";
                    serialNumber++;
                }
            }

            return rowsHtml;
        }

        // ----- allocation planning -----

        private sealed class PaymentPlan
        {
            public string ErrorCode { get; set; }
            public string ErrorMessage { get; set; }
            public Enrollment Enrollment { get; set; }
            public decimal OutstandingBefore { get; set; }
            public decimal UnallocatedAmount { get; set; }
            public Dictionary<Guid, FeeInvoice> InvoicesById { get; set; } = new Dictionary<Guid, FeeInvoice>();
            public List<PlannedAllocation> Allocations { get; set; } = new List<PlannedAllocation>();
            public List<FeeRuleDiscount> RuleDiscounts { get; set; } = new List<FeeRuleDiscount>();

            // Invoices synthesized for advance billing (2026-07-17) -- not yet in the database;
            // CreateAsync AddAsync's each one. Empty on the ordinary "pay what's already open"
            // path.
            public List<FeeInvoice> NewlyCreatedInvoices { get; set; } = new List<FeeInvoice>();
        }

        private sealed class PlannedAllocation
        {
            public Guid FeeInvoiceId { get; set; }
            public decimal Amount { get; set; }
            public bool SettlesInvoice { get; set; }
            public bool IsNewlyGenerated { get; set; }
        }

        // What one advance-billing extension pass produced.
        private sealed class AdvanceBillingResult
        {
            public string ErrorCode { get; set; }
            public string ErrorMessage { get; set; }
            public List<FeeInvoice> Invoices { get; set; } = new List<FeeInvoice>();
        }

        // Shared by preview and confirm so the confirmed writes are exactly the previewed plan:
        // pass 1 finds the fully-settled set (FIFO, pre-discount), the rule engine proposes
        // discounts against that set, pass 2 re-allocates against the discounted balances.
        // Preview never persists -- balances are simulated in local dictionaries, tracked
        // entities are only mutated in CreateAsync.
        private async Task<PaymentPlan> BuildPlanAsync(CreateFeePaymentCommand command, CancellationToken cancellationToken)
        {
            var plan = new PaymentPlan();

            // Normalize once, up front -- every downstream use (the FeeRule lookup query below,
            // and the FeePayment.PaymentDate assignment in CreateAsync) then sees a UTC-kind value.
            command.PaymentDate = DateTimeHelper.AsUtcDate(command.PaymentDate);

            var validationResult = _createValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                plan.ErrorCode = ResponseCodes.ValidationError;
                plan.ErrorMessage = BuildValidationErrorMessage(validationResult);
                return plan;
            }

            var enrollment = await _unitOfWork.Enrollments.GetWithDetailsAsync(command.EnrollmentId, cancellationToken);
            if (enrollment == null)
            {
                plan.ErrorCode = ResponseCodes.NotFound;
                plan.ErrorMessage = "Enrollment with id '" + command.EnrollmentId + "' was not found.";
                return plan;
            }

            plan.Enrollment = enrollment;

            // No early "nothing open" guard (removed 2026-07-17): a brand-new enrollment with
            // zero invoices ever generated must still be payable -- it just means
            // outstandingBefore starts at 0, which the AllowAdvanceBilling branch below handles
            // (bills the first N months from scratch) or reports clearly if advance billing is
            // off.
            var openInvoices = await _unitOfWork.FeeInvoices.GetOpenByEnrollmentAsync(command.EnrollmentId, cancellationToken);

            decimal outstandingBefore = 0m;
            var balancesByInvoice = new Dictionary<Guid, decimal>();
            foreach (var invoice in openInvoices)
            {
                var balance = invoice.NetAmount - invoice.PaidAmount;
                balancesByInvoice[invoice.Id] = balance;
                outstandingBefore += balance;
                plan.InvoicesById[invoice.Id] = invoice;
            }

            plan.OutstandingBefore = outstandingBefore;

            var workingInvoices = new List<FeeInvoice>(openInvoices);

            if (command.Amount > outstandingBefore)
            {
                if (!command.AllowAdvanceBilling)
                {
                    plan.ErrorCode = ResponseCodes.ValidationError;
                    plan.ErrorMessage = "Amount exceeds the total outstanding of " + outstandingBefore.ToString("F2") + " (advance billing is disabled for this payment).";
                    return plan;
                }

                var amountNeeded = command.Amount - outstandingBefore;
                var advanceResult = await BuildAdvanceInvoicesAsync(enrollment, amountNeeded, null, command.PaymentDate, cancellationToken);
                if (advanceResult.ErrorCode != null)
                {
                    plan.ErrorCode = advanceResult.ErrorCode;
                    plan.ErrorMessage = advanceResult.ErrorMessage;
                    return plan;
                }

                foreach (var newInvoice in advanceResult.Invoices)
                {
                    balancesByInvoice[newInvoice.Id] = newInvoice.NetAmount;
                    outstandingBefore += newInvoice.NetAmount;
                    plan.InvoicesById[newInvoice.Id] = newInvoice;
                    workingInvoices.Add(newInvoice);
                    plan.NewlyCreatedInvoices.Add(newInvoice);
                }

                plan.OutstandingBefore = outstandingBefore;

                if (command.Amount > outstandingBefore)
                {
                    plan.ErrorCode = ResponseCodes.ValidationError;
                    plan.ErrorMessage = "Amount exceeds the total outstanding even after billing " + plan.NewlyCreatedInvoices.Count + " month(s) in advance (at most " + MaxAdvanceBillingMonths + " months can be billed ahead in one payment). Outstanding is now " + outstandingBefore.ToString("F2") + ".";
                    return plan;
                }
            }

            // Pass 1: which invoices does this payment fully settle? That set is what
            // payment-time rules evaluate against (F9). The discount a rule earns counts toward
            // the settlement itself -- tendering (3 months' total - the rule's discount) still
            // settles 3 months. Without that, the discounted amount the preview tells the
            // cashier to collect could never re-earn the discount on confirm: the rule only
            // fired when the FULL pre-discount total was tendered, and confirming that full
            // total was then rejected as an over-payment -- so in practice no rule ever applied.
            // Mechanics: try the largest FIFO prefix of open invoices first; for each candidate
            // set, ask the rule engine what it would grant, and accept the first (largest) set
            // where tendered amount + earned discounts covers the set's pre-discount balances.
            if (command.ApplyRuleDiscounts)
            {
                var activeRules = await _unitOfWork.FeeRules.GetActiveRulesAsync(FeeRuleTrigger.OnPayment, command.PaymentDate, cancellationToken);
                if (activeRules.Count > 0)
                {
                    var settleableInvoices = new List<FeeInvoice>();
                    foreach (var invoice in workingInvoices)
                    {
                        if (balancesByInvoice[invoice.Id] > 0m)
                        {
                            settleableInvoices.Add(invoice);
                        }
                    }

                    for (var settledCount = settleableInvoices.Count; settledCount >= 1; settledCount--)
                    {
                        var context = new FeeRuleContext
                        {
                            EnrollmentId = enrollment.Id,
                            AcademicClassId = enrollment.ClassSection.AcademicClassId,
                            PaymentDate = command.PaymentDate
                        };

                        decimal candidateSetBalance = 0m;
                        for (var i = 0; i < settledCount; i++)
                        {
                            var invoice = settleableInvoices[i];
                            candidateSetBalance += balancesByInvoice[invoice.Id];

                            var invoiceContext = BuildInvoiceContext(invoice, balancesByInvoice[invoice.Id]);
                            context.FullySettledInvoices.Add(invoiceContext);
                        }

                        var ruleDiscounts = FeeRuleEngine.Evaluate(activeRules, context);

                        // Cap each discount at the invoice's remaining balance (tracked across
                        // this candidate's own discounts) so a stacked percentage can never push
                        // a balance negative.
                        var cappedDiscounts = new List<FeeRuleDiscount>();
                        decimal discountTotal = 0m;
                        var candidateBalances = new Dictionary<Guid, decimal>(balancesByInvoice);
                        foreach (var ruleDiscount in ruleDiscounts)
                        {
                            var currentBalance = candidateBalances[ruleDiscount.FeeInvoiceId];
                            var cappedAmount = Math.Min(ruleDiscount.Amount, currentBalance);
                            if (cappedAmount <= 0m)
                            {
                                continue;
                            }

                            ruleDiscount.Amount = cappedAmount;
                            candidateBalances[ruleDiscount.FeeInvoiceId] = currentBalance - cappedAmount;
                            cappedDiscounts.Add(ruleDiscount);
                            discountTotal += cappedAmount;
                        }

                        // Affordability check: does the money on the table (tendered + earned
                        // discounts) cover this candidate set at pre-discount balances?
                        if (command.Amount + discountTotal < candidateSetBalance)
                        {
                            continue;
                        }

                        foreach (var cappedDiscount in cappedDiscounts)
                        {
                            balancesByInvoice[cappedDiscount.FeeInvoiceId] -= cappedDiscount.Amount;
                            plan.RuleDiscounts.Add(cappedDiscount);
                        }

                        break;
                    }
                }
            }

            // Pass 2: final FIFO allocation against the (possibly discounted) balances.
            var remainingToAllocate = command.Amount;
            foreach (var invoice in workingInvoices)
            {
                if (remainingToAllocate <= 0m)
                {
                    break;
                }

                var balance = balancesByInvoice[invoice.Id];
                if (balance <= 0m)
                {
                    continue;
                }

                var allocationAmount = Math.Min(remainingToAllocate, balance);
                var plannedAllocation = new PlannedAllocation
                {
                    FeeInvoiceId = invoice.Id,
                    Amount = allocationAmount,
                    SettlesInvoice = allocationAmount == balance,
                    IsNewlyGenerated = plan.NewlyCreatedInvoices.Contains(invoice)
                };
                plan.Allocations.Add(plannedAllocation);

                remainingToAllocate -= allocationAmount;
            }

            plan.UnallocatedAmount = remainingToAllocate;
            return plan;
        }

        // Bills consecutive future months onto this enrollment (same composition as regular
        // generation -- FeeInvoiceFactory keeps the two paths identical) until the running
        // total covers amountNeeded or the MaxAdvanceBillingMonths cap is hit. Returns whatever
        // it managed to build even if short of amountNeeded -- the caller re-checks the total
        // and reports a precise error, so this helper never needs to duplicate that message.
        // Exactly one of amountNeeded/targetMonthCount should be supplied: amountNeeded drives
        // the payment-confirmation path (keep billing until the tendered amount is covered),
        // targetMonthCount drives the read-only quote (§ GetAdvanceQuoteAsync -- bill exactly
        // this many additional months so the caller can show "X months costs Rs Y").
        private async Task<AdvanceBillingResult> BuildAdvanceInvoicesAsync(Enrollment enrollment, decimal? amountNeeded, int? targetMonthCount, DateTime referenceDate, CancellationToken cancellationToken)
        {
            var result = new AdvanceBillingResult();

            var academicClassId = enrollment.ClassSection.AcademicClassId;
            var academicYearId = enrollment.ClassSection.AcademicClass.AcademicYearId;

            var feeStructure = await _unitOfWork.FeeStructures.GetByAcademicClassIdAsync(academicClassId, cancellationToken);
            if (feeStructure == null || feeStructure.Status != RecordStatus.Active)
            {
                result.ErrorCode = ResponseCodes.ValidationError;
                result.ErrorMessage = "This enrollment's class has no active fee structure -- future months cannot be billed in advance.";
                return result;
            }

            var academicYear = await _unitOfWork.AcademicYears.GetByIdAsync(academicYearId, cancellationToken);

            var enrollmentIds = new List<Guid> { enrollment.Id };
            var discounts = await _unitOfWork.Enrollments.GetDiscountsByEnrollmentIdsAsync(enrollmentIds, cancellationToken);
            var scholarships = await _unitOfWork.Enrollments.GetScholarshipsByEnrollmentIdsAsync(enrollmentIds, cancellationToken);
            var feeSelections = await _unitOfWork.Enrollments.GetFeeSelectionsByEnrollmentIdsAsync(enrollmentIds, cancellationToken);
            var selectedItemIds = new List<Guid>();
            foreach (var feeSelection in feeSelections)
            {
                selectedItemIds.Add(feeSelection.FeeStructureItemId);
            }

            // Every non-cancelled invoice this enrollment already has (Draft included), with
            // Lines: anchors the annual-installment schedule (via each item's actual remaining
            // balance -- FeeInvoiceFactory), flags OneTime items as already charged, and tells
            // us which billing month comes next so we never collide with a period someone
            // already generated (a bulk-generation Draft included).
            var earlierInvoices = new List<FeeInvoice>(await _unitOfWork.FeeInvoices.GetByEnrollmentIdsWithLinesAsync(new List<Guid> { enrollment.Id }, cancellationToken));

            var nextYear = referenceDate.Year;
            var nextMonth = referenceDate.Month;
            foreach (var earlierInvoice in earlierInvoices)
            {
                if (earlierInvoice.BillingYear > nextYear
                    || (earlierInvoice.BillingYear == nextYear && earlierInvoice.BillingMonth >= nextMonth))
                {
                    nextYear = earlierInvoice.BillingYear;
                    nextMonth = earlierInvoice.BillingMonth + 1;
                    if (nextMonth > 12)
                    {
                        nextMonth = 1;
                        nextYear++;
                    }
                }
            }

            var feeLabels = await LoadFeeLabelMapAsync(cancellationToken);

            var invoiceNoPrefix = FeeInvoiceFactory.InvoiceNoPrefix + nextYear;
            var existingInvoiceNos = await _unitOfWork.FeeInvoices.GetInvoiceNosByPrefixAsync(invoiceNoPrefix, cancellationToken);
            var invoiceNos = new List<string>(existingInvoiceNos);
            var invoiceNoYear = nextYear;

            decimal runningNewOutstanding = 0m;
            var billingYear = nextYear;
            var billingMonth = nextMonth;

            for (var monthsGenerated = 0; monthsGenerated < MaxAdvanceBillingMonths; monthsGenerated++)
            {
                if (amountNeeded.HasValue && runningNewOutstanding >= amountNeeded.Value)
                {
                    break;
                }

                if (targetMonthCount.HasValue && monthsGenerated >= targetMonthCount.Value)
                {
                    break;
                }

                if (billingYear != invoiceNoYear)
                {
                    invoiceNoYear = billingYear;
                    invoiceNoPrefix = FeeInvoiceFactory.InvoiceNoPrefix + billingYear;
                    var existingForYear = await _unitOfWork.FeeInvoices.GetInvoiceNosByPrefixAsync(invoiceNoPrefix, cancellationToken);
                    invoiceNos = new List<string>(existingForYear);
                }

                var pendingAdjustments = await _unitOfWork.FeeInvoices.GetPendingAdjustmentsForPeriodAsync(enrollmentIds, billingYear, billingMonth, cancellationToken);
                var dueDate = await ResolveDueDateAsync(billingYear, billingMonth, cancellationToken);
                var invoiceNo = NumberSequenceHelper.Next(invoiceNoPrefix, invoiceNos, 4);
                invoiceNos.Add(invoiceNo);

                var newInvoice = FeeInvoiceFactory.BuildInvoice(
                    Guid.NewGuid(),
                    enrollment,
                    academicYear,
                    feeStructure,
                    earlierInvoices,
                    discounts,
                    scholarships,
                    selectedItemIds,
                    pendingAdjustments,
                    billingYear,
                    billingMonth,
                    dueDate,
                    invoiceNo,
                    stampAdjustments: false,
                    feeLabels);

                // Billed and settled the moment it's created -- there is no Draft review step
                // for advance-billed months (same as Finalize).
                newInvoice.Status = FeeInvoiceStatus.Generated;
                newInvoice.GeneratedTs = DateTime.UtcNow;

                result.Invoices.Add(newInvoice);
                earlierInvoices.Add(newInvoice);
                runningNewOutstanding += newInvoice.NetAmount;

                billingMonth++;
                if (billingMonth > 12)
                {
                    billingMonth = 1;
                    billingYear++;
                }
            }

            return result;
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

        private static FeeRuleInvoiceContext BuildInvoiceContext(FeeInvoice invoice, decimal remainingBalance)
        {
            var invoiceContext = new FeeRuleInvoiceContext
            {
                FeeInvoiceId = invoice.Id,
                BillingYear = invoice.BillingYear,
                BillingMonth = invoice.BillingMonth,
                DueDate = invoice.DueDate,
                RemainingBalance = remainingBalance
            };

            foreach (var line in invoice.Lines)
            {
                if (line.Source != FeeLineSource.StructureItem && line.Source != FeeLineSource.AnnualInstallment)
                {
                    continue;
                }

                invoiceContext.RecurringSubtotal += line.Amount;

                if (!string.IsNullOrWhiteSpace(line.FeeCategoryCode))
                {
                    invoiceContext.RecurringSubtotalByCategory.TryGetValue(line.FeeCategoryCode, out var categoryAmount);
                    invoiceContext.RecurringSubtotalByCategory[line.FeeCategoryCode] = categoryAmount + line.Amount;
                }
            }

            return invoiceContext;
        }

        // Merged label map for every catalog a fee-invoice line's code can come from -- fee
        // categories plus the discount/scholarship/adjustment type catalogs (their code
        // namespaces are distinct by convention).
        private async Task<Dictionary<string, string>> LoadFeeLabelMapAsync(CancellationToken cancellationToken)
        {
            var labelsByCode = ConfigLabelHelper.BuildLabelMap(await _unitOfWork.Configs.GetByTypeCodeAsync(ConfigTypeCodes.FeeCategory, cancellationToken));
            ConfigLabelHelper.MergeLabelMap(labelsByCode, await _unitOfWork.Configs.GetByTypeCodeAsync(ConfigTypeCodes.DiscountType, cancellationToken));
            ConfigLabelHelper.MergeLabelMap(labelsByCode, await _unitOfWork.Configs.GetByTypeCodeAsync(ConfigTypeCodes.ScholarshipType, cancellationToken));
            ConfigLabelHelper.MergeLabelMap(labelsByCode, await _unitOfWork.Configs.GetByTypeCodeAsync(ConfigTypeCodes.FeeAdjustmentType, cancellationToken));
            return labelsByCode;
        }

        private static FeePaymentPreviewDto BuildPreviewDto(CreateFeePaymentCommand command, PaymentPlan plan, IReadOnlyDictionary<string, string> labelsByCode)
        {
            var previewDto = new FeePaymentPreviewDto
            {
                EnrollmentId = command.EnrollmentId,
                Amount = command.Amount,
                OutstandingBefore = plan.OutstandingBefore,
                UnallocatedAmount = plan.UnallocatedAmount,
                MonthsBilledInAdvance = plan.NewlyCreatedInvoices.Count
            };

            decimal totalDiscount = 0m;
            foreach (var ruleDiscount in plan.RuleDiscounts)
            {
                totalDiscount += ruleDiscount.Amount;

                var discountDto = new FeeRuleDiscountDto
                {
                    FeeRuleId = ruleDiscount.FeeRuleId,
                    RuleCode = ruleDiscount.RuleCode,
                    RuleName = ruleDiscount.RuleName,
                    FeeInvoiceId = ruleDiscount.FeeInvoiceId,
                    Amount = ruleDiscount.Amount,
                    Description = ruleDiscount.Description
                };
                previewDto.RuleDiscounts.Add(discountDto);
            }

            previewDto.TotalRuleDiscount = totalDiscount;

            decimal allocatedTotal = 0m;
            foreach (var plannedAllocation in plan.Allocations)
            {
                allocatedTotal += plannedAllocation.Amount;

                var invoice = plan.InvoicesById[plannedAllocation.FeeInvoiceId];
                var allocationDto = new FeePaymentAllocationDto
                {
                    FeeInvoiceId = plannedAllocation.FeeInvoiceId,
                    InvoiceNo = invoice.InvoiceNo,
                    BillingYear = invoice.BillingYear,
                    BillingMonth = invoice.BillingMonth,
                    Amount = plannedAllocation.Amount,
                    SettlesInvoice = plannedAllocation.SettlesInvoice,
                    IsNewlyGenerated = plannedAllocation.IsNewlyGenerated
                };

                foreach (var line in invoice.Lines)
                {
                    var allocationLineDto = new FeePaymentAllocationLineDto
                    {
                        Source = line.Source,
                        FeeCategoryCode = line.FeeCategoryCode,
                        FeeCategoryLabel = ConfigLabelHelper.Resolve(labelsByCode, line.FeeCategoryCode),
                        Description = line.Description,
                        Amount = line.Amount
                    };
                    allocationDto.Lines.Add(allocationLineDto);
                }

                previewDto.Allocations.Add(allocationDto);
            }

            previewDto.OutstandingAfter = plan.OutstandingBefore - totalDiscount - allocatedTotal;
            return previewDto;
        }

        private static void RederivePaymentStatus(FeeInvoice invoice)
        {
            if (invoice.PaidAmount >= invoice.NetAmount)
            {
                invoice.Status = FeeInvoiceStatus.Paid;
                return;
            }

            if (invoice.PaidAmount > 0m)
            {
                invoice.Status = FeeInvoiceStatus.PartiallyPaid;
                return;
            }

            // Fully reversed: back to awaiting payment, overdue-aware (F6).
            invoice.Status = invoice.DueDate.Date < DateTime.UtcNow.Date
                ? FeeInvoiceStatus.Pending
                : FeeInvoiceStatus.Generated;
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
