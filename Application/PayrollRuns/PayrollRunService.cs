using Application.Common.Helpers;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Payroll;
using Application.Payroll.Dtos;
using Application.PayrollRuns.Commands;
using Application.PayrollRuns.Dtos;
using Application.PayrollRuns.Queries;
using Application.PayrollRuns.Validators;
using Domain.Common.Filters;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using FluentValidation.Results;

namespace Application.PayrollRuns
{
    public class PayrollRunService : IPayrollRunService
    {
        private const string SlipNoPrefix = "SAL";

        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly CreatePayrollRunCommandValidator _createValidator;
        private readonly SalarySlipLineInputValidator _lineInputValidator;
        private readonly UpdateSalarySlipLineCommandValidator _updateLineValidator;

        public PayrollRunService(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            CreatePayrollRunCommandValidator createValidator,
            SalarySlipLineInputValidator lineInputValidator,
            UpdateSalarySlipLineCommandValidator updateLineValidator)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _createValidator = createValidator;
            _lineInputValidator = lineInputValidator;
            _updateLineValidator = updateLineValidator;
        }

        // One monthly payroll execution: snapshots every payable employee's compensation plan
        // (the revision effective for the month), tax, due loan EMIs, and Pending monthly
        // adjustments into Draft slips -- all in a single SaveChangesAsync (P1-P4).
        public async Task<CommonResponse<PayrollGenerationResultDto>> CreateRunAsync(CreatePayrollRunCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _createValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<PayrollGenerationResultDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var fiscalYear = await _unitOfWork.FiscalYears.GetByIdAsync(command.FiscalYearId, cancellationToken);
            if (fiscalYear == null)
            {
                var notFoundResponse = CommonResponse<PayrollGenerationResultDto>.Fail(ResponseCodes.NotFound, "Fiscal year with id '" + command.FiscalYearId + "' was not found.");
                return notFoundResponse;
            }

            var runExists = await _unitOfWork.PayrollRuns.ExistsForPeriodAsync(command.FiscalYearId, command.MonthIndex, cancellationToken);
            if (runExists)
            {
                var conflictResponse = CommonResponse<PayrollGenerationResultDto>.Fail(ResponseCodes.Conflict, "A payroll run for that fiscal month already exists -- cancel it first to regenerate.");
                return conflictResponse;
            }

            var employees = await _unitOfWork.Employees.GetPayrollEligibleEmployeesAsync(cancellationToken);
            if (employees.Count == 0)
            {
                var noEmployeesResponse = CommonResponse<PayrollGenerationResultDto>.Fail(ResponseCodes.ValidationError, "No payable employees with a compensation plan were found.");
                return noEmployeesResponse;
            }

            var employeeIds = new List<Guid>();
            foreach (var employee in employees)
            {
                employeeIds.Add(employee.Id);
            }

            var approvedLoans = await _unitOfWork.Employees.GetApprovedLoansByEmployeeIdsAsync(employeeIds, cancellationToken);
            var loansByEmployee = new Dictionary<Guid, List<EmployeeLoan>>();
            foreach (var loan in approvedLoans)
            {
                if (!loansByEmployee.TryGetValue(loan.EmployeeId, out var employeeLoans))
                {
                    employeeLoans = new List<EmployeeLoan>();
                    loansByEmployee[loan.EmployeeId] = employeeLoans;
                }

                employeeLoans.Add(loan);
            }

            var pendingAdjustments = await _unitOfWork.Employees.GetPendingSalaryAdjustmentsForPeriodAsync(command.FiscalYearId, command.MonthIndex, cancellationToken);
            var adjustmentsByEmployee = new Dictionary<Guid, List<SalaryAdjustment>>();
            foreach (var adjustment in pendingAdjustments)
            {
                if (!adjustmentsByEmployee.TryGetValue(adjustment.EmployeeId, out var employeeAdjustments))
                {
                    employeeAdjustments = new List<SalaryAdjustment>();
                    adjustmentsByEmployee[adjustment.EmployeeId] = employeeAdjustments;
                }

                employeeAdjustments.Add(adjustment);
            }

            var insuranceTypeCaps = await BuildInsuranceTypeCapsAsync(cancellationToken);
            var payrollLabels = await LoadPayrollLabelMapAsync(cancellationToken);
            var calculationConfigByCode = await LoadSalaryLineCalculationConfigAsync(cancellationToken);
            var taxSlabsByAssessmentType = new Dictionary<TaxAssessmentType, IReadOnlyList<TaxSlab>>();

            var slipNoPrefix = SlipNoPrefix + fiscalYear.Code;
            var existingSlipNos = await _unitOfWork.PayrollRuns.GetSlipNosByPrefixAsync(slipNoPrefix, cancellationToken);
            var slipNos = new List<string>(existingSlipNos);

            var run = new PayrollRun
            {
                Id = Guid.NewGuid(),
                FiscalYearId = command.FiscalYearId,
                MonthIndex = command.MonthIndex,
                Status = PayrollRunStatus.Draft,
                GeneratedTs = DateTime.UtcNow,
                Remarks = command.Remarks?.Trim(),
                FiscalYear = fiscalYear
            };

            var result = new PayrollGenerationResultDto();

            foreach (var employee in employees)
            {
                var employeeName = BuildFullName(employee.FirstName, employee.MiddleName, employee.LastName);

                var salary = ResolveSalaryRevisionForMonth(employee, fiscalYear, command.MonthIndex);
                if (salary == null)
                {
                    result.Skipped.Add(new PayrollSkipDto
                    {
                        EmployeeId = employee.Id,
                        EmployeeName = employeeName,
                        Reason = "No compensation-plan revision is effective for this month."
                    });
                    continue;
                }

                if (!taxSlabsByAssessmentType.TryGetValue(salary.AssessmentType, out var taxSlabs))
                {
                    taxSlabs = await _unitOfWork.FiscalYears.GetTaxSlabsAsync(fiscalYear.Id, salary.AssessmentType, cancellationToken);
                    taxSlabsByAssessmentType[salary.AssessmentType] = taxSlabs;
                }

                if (taxSlabs.Count == 0)
                {
                    result.Skipped.Add(new PayrollSkipDto
                    {
                        EmployeeId = employee.Id,
                        EmployeeName = employeeName,
                        Reason = "Fiscal year '" + fiscalYear.Code + "' has no tax slabs for assessment type '" + salary.AssessmentType + "'."
                    });
                    continue;
                }

                var components = salary.Components.ToList();
                var deductions = salary.Deductions.ToList();

                var taxCalculation = TaxCalculator.CalculateFromSalary(
                    components,
                    deductions,
                    salary.InsurancePremiums.ToList(),
                    insuranceTypeCaps,
                    fiscalYear.RetirementExemptionCapAmount,
                    taxSlabs,
                    null,
                    calculationConfigByCode);

                var months = MonthlyBreakdownCalculator.Build(components, deductions, salary.EffectiveFromDate, fiscalYear, taxCalculation.AnnualTax, payrollLabels, calculationConfigByCode);
                var monthRow = months[command.MonthIndex - 1];

                loansByEmployee.TryGetValue(employee.Id, out var employeeLoans);
                adjustmentsByEmployee.TryGetValue(employee.Id, out var employeeAdjustments);

                var slipNo = NumberSequenceHelper.Next(slipNoPrefix, slipNos, 4);
                slipNos.Add(slipNo);

                var slip = BuildSlip(
                    run,
                    employee,
                    salary,
                    monthRow,
                    components,
                    employeeLoans ?? new List<EmployeeLoan>(),
                    employeeAdjustments ?? new List<SalaryAdjustment>(),
                    slipNo,
                    payrollLabels);

                run.Slips.Add(slip);
            }

            if (run.Slips.Count == 0)
            {
                var nothingResponse = CommonResponse<PayrollGenerationResultDto>.Fail(ResponseCodes.ValidationError, "No salary slips could be generated -- every employee was skipped (see reasons on retry after fixing configuration).");
                return nothingResponse;
            }

            await _unitOfWork.PayrollRuns.AddAsync(run, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            result.Run = PayrollRunMapper.ToDto(run, includeSlips: true);

            var successResponse = CommonResponse<PayrollGenerationResultDto>.Success(result, "Generated a Draft payroll run with " + run.Slips.Count + " slip(s); skipped " + result.Skipped.Count + " employee(s).");
            return successResponse;
        }

        // In-place regeneration of a Draft run -- the sanctioned way to pick up compensation-plan
        // edits, tax-slab changes, new loans, or new Pending adjustments made AFTER generation,
        // without cancelling the run. Semantics:
        //   - Draft slips are rebuilt from the CURRENT configuration (their generated lines are
        //     replaced; Manual lines -- the admin's explicit edits -- are preserved).
        //   - Adjustments the run had consumed are re-pended first, so the rebuild re-applies the
        //     current Pending set (including ones added since generation).
        //   - Individually-cancelled slips stay cancelled (that was a deliberate admin decision).
        //   - Employees who became payroll-eligible since generation get a new slip; employees no
        //     longer eligible have their Draft slip cancelled.
        public async Task<CommonResponse<PayrollGenerationResultDto>> RefreshRunAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var run = await _unitOfWork.PayrollRuns.GetByIdWithSlipsAsync(id, cancellationToken);
            if (run == null)
            {
                var notFoundResponse = CommonResponse<PayrollGenerationResultDto>.Fail(ResponseCodes.NotFound, "Payroll run with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            if (run.Status != PayrollRunStatus.Draft)
            {
                var wrongStateResponse = CommonResponse<PayrollGenerationResultDto>.Fail(ResponseCodes.Conflict, "Only a Draft run can be refreshed; this one is '" + run.Status + "'.");
                return wrongStateResponse;
            }

            var fiscalYear = run.FiscalYear;

            var employees = await _unitOfWork.Employees.GetPayrollEligibleEmployeesAsync(cancellationToken);

            var employeeIds = new List<Guid>();
            foreach (var employee in employees)
            {
                employeeIds.Add(employee.Id);
            }

            var approvedLoans = await _unitOfWork.Employees.GetApprovedLoansByEmployeeIdsAsync(employeeIds, cancellationToken);
            var loansByEmployee = new Dictionary<Guid, List<EmployeeLoan>>();
            foreach (var loan in approvedLoans)
            {
                if (!loansByEmployee.TryGetValue(loan.EmployeeId, out var employeeLoans))
                {
                    employeeLoans = new List<EmployeeLoan>();
                    loansByEmployee[loan.EmployeeId] = employeeLoans;
                }

                employeeLoans.Add(loan);
            }

            // Re-pend everything this run's slips consumed, then merge with the period's still-
            // Pending adjustments. The pending query runs against the database, where the just-
            // re-pended rows still read Applied, so the two lists can't overlap -- the id set
            // guards against that ever changing.
            var slipIds = new List<Guid>();
            foreach (var slip in run.Slips)
            {
                slipIds.Add(slip.Id);
            }

            var consumedAdjustments = await _unitOfWork.Employees.GetSalaryAdjustmentsAppliedToSlipsAsync(slipIds, cancellationToken);
            foreach (var consumedAdjustment in consumedAdjustments)
            {
                consumedAdjustment.Status = AdjustmentStatus.Pending;
                consumedAdjustment.AppliedSalarySlipId = null;
            }

            var pendingAdjustments = await _unitOfWork.Employees.GetPendingSalaryAdjustmentsForPeriodAsync(run.FiscalYearId, run.MonthIndex, cancellationToken);

            var adjustmentsByEmployee = new Dictionary<Guid, List<SalaryAdjustment>>();
            var seenAdjustmentIds = new HashSet<Guid>();
            var mergedAdjustments = new List<SalaryAdjustment>();
            mergedAdjustments.AddRange(consumedAdjustments);
            mergedAdjustments.AddRange(pendingAdjustments);
            foreach (var adjustment in mergedAdjustments)
            {
                if (!seenAdjustmentIds.Add(adjustment.Id))
                {
                    continue;
                }

                if (!adjustmentsByEmployee.TryGetValue(adjustment.EmployeeId, out var employeeAdjustments))
                {
                    employeeAdjustments = new List<SalaryAdjustment>();
                    adjustmentsByEmployee[adjustment.EmployeeId] = employeeAdjustments;
                }

                employeeAdjustments.Add(adjustment);
            }

            var insuranceTypeCaps = await BuildInsuranceTypeCapsAsync(cancellationToken);
            var payrollLabels = await LoadPayrollLabelMapAsync(cancellationToken);
            var calculationConfigByCode = await LoadSalaryLineCalculationConfigAsync(cancellationToken);
            var taxSlabsByAssessmentType = new Dictionary<TaxAssessmentType, IReadOnlyList<TaxSlab>>();

            var slipNoPrefix = SlipNoPrefix + fiscalYear.Code;
            var existingSlipNos = await _unitOfWork.PayrollRuns.GetSlipNosByPrefixAsync(slipNoPrefix, cancellationToken);
            var slipNos = new List<string>(existingSlipNos);

            var slipsByEmployee = new Dictionary<Guid, SalarySlip>();
            foreach (var slip in run.Slips)
            {
                slipsByEmployee[slip.EmployeeId] = slip;
            }

            var result = new PayrollGenerationResultDto();
            var processedEmployeeIds = new HashSet<Guid>();

            foreach (var employee in employees)
            {
                var employeeName = BuildFullName(employee.FirstName, employee.MiddleName, employee.LastName);
                processedEmployeeIds.Add(employee.Id);

                slipsByEmployee.TryGetValue(employee.Id, out var existingSlip);

                if (existingSlip != null && existingSlip.Status == SalarySlipStatus.Cancelled)
                {
                    result.Skipped.Add(new PayrollSkipDto
                    {
                        EmployeeId = employee.Id,
                        EmployeeName = employeeName,
                        Reason = "Slip was individually cancelled; refresh keeps it cancelled."
                    });
                    continue;
                }

                var salary = ResolveSalaryRevisionForMonth(employee, fiscalYear, run.MonthIndex);
                if (salary == null)
                {
                    if (existingSlip != null)
                    {
                        existingSlip.Status = SalarySlipStatus.Cancelled;
                    }

                    result.Skipped.Add(new PayrollSkipDto
                    {
                        EmployeeId = employee.Id,
                        EmployeeName = employeeName,
                        Reason = "No compensation-plan revision is effective for this month." + (existingSlip != null ? " The existing Draft slip was cancelled." : string.Empty)
                    });
                    continue;
                }

                if (!taxSlabsByAssessmentType.TryGetValue(salary.AssessmentType, out var taxSlabs))
                {
                    taxSlabs = await _unitOfWork.FiscalYears.GetTaxSlabsAsync(fiscalYear.Id, salary.AssessmentType, cancellationToken);
                    taxSlabsByAssessmentType[salary.AssessmentType] = taxSlabs;
                }

                if (taxSlabs.Count == 0)
                {
                    if (existingSlip != null)
                    {
                        existingSlip.Status = SalarySlipStatus.Cancelled;
                    }

                    result.Skipped.Add(new PayrollSkipDto
                    {
                        EmployeeId = employee.Id,
                        EmployeeName = employeeName,
                        Reason = "Fiscal year '" + fiscalYear.Code + "' has no tax slabs for assessment type '" + salary.AssessmentType + "'." + (existingSlip != null ? " The existing Draft slip was cancelled." : string.Empty)
                    });
                    continue;
                }

                var components = salary.Components.ToList();
                var deductions = salary.Deductions.ToList();

                var taxCalculation = TaxCalculator.CalculateFromSalary(
                    components,
                    deductions,
                    salary.InsurancePremiums.ToList(),
                    insuranceTypeCaps,
                    fiscalYear.RetirementExemptionCapAmount,
                    taxSlabs,
                    null,
                    calculationConfigByCode);

                var months = MonthlyBreakdownCalculator.Build(components, deductions, salary.EffectiveFromDate, fiscalYear, taxCalculation.AnnualTax, payrollLabels, calculationConfigByCode);
                var monthRow = months[run.MonthIndex - 1];

                loansByEmployee.TryGetValue(employee.Id, out var employeeLoans);
                adjustmentsByEmployee.TryGetValue(employee.Id, out var employeeAdjustments);

                if (existingSlip == null)
                {
                    var slipNo = NumberSequenceHelper.Next(slipNoPrefix, slipNos, 4);
                    slipNos.Add(slipNo);

                    var newSlip = BuildSlip(
                        run,
                        employee,
                        salary,
                        monthRow,
                        components,
                        employeeLoans ?? new List<EmployeeLoan>(),
                        employeeAdjustments ?? new List<SalaryAdjustment>(),
                        slipNo,
                        payrollLabels);

                    run.Slips.Add(newSlip);
                    // Explicit Add: relying on collection fixup would track the slip (pre-set
                    // Guid id) as Unchanged and skip its INSERT -- see IPayrollRunRepository.
                    await _unitOfWork.PayrollRuns.AddSlipAsync(newSlip, cancellationToken);
                    continue;
                }

                RebuildSlip(
                    existingSlip,
                    salary,
                    monthRow,
                    components,
                    employeeLoans ?? new List<EmployeeLoan>(),
                    employeeAdjustments ?? new List<SalaryAdjustment>(),
                    payrollLabels);
            }

            // Employees whose slip survived generation but who are no longer payroll-eligible
            // (status change, plan removed): their Draft slip is cancelled rather than left as a
            // stale snapshot.
            foreach (var slip in run.Slips)
            {
                if (slip.Status != SalarySlipStatus.Draft || processedEmployeeIds.Contains(slip.EmployeeId))
                {
                    continue;
                }

                slip.Status = SalarySlipStatus.Cancelled;

                var slipEmployeeName = slip.Employee != null
                    ? BuildFullName(slip.Employee.FirstName, slip.Employee.MiddleName, slip.Employee.LastName)
                    : slip.EmployeeId.ToString();

                result.Skipped.Add(new PayrollSkipDto
                {
                    EmployeeId = slip.EmployeeId,
                    EmployeeName = slipEmployeeName,
                    Reason = "Employee is no longer payroll-eligible; the Draft slip was cancelled."
                });
            }

            run.GeneratedTs = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            result.Run = PayrollRunMapper.ToDto(run, includeSlips: true);

            var draftSlipCount = 0;
            foreach (var slip in run.Slips)
            {
                if (slip.Status == SalarySlipStatus.Draft)
                {
                    draftSlipCount++;
                }
            }

            var successResponse = CommonResponse<PayrollGenerationResultDto>.Success(result, "Refreshed the Draft payroll run: " + draftSlipCount + " slip(s) rebuilt from the current configuration; " + result.Skipped.Count + " employee(s) skipped.");
            return successResponse;
        }

        public async Task<CommonResponse<PaginatedResponse<PayrollRunDto>>> GetRunsAsync(GetPayrollRunsQuery query, CancellationToken cancellationToken = default)
        {
            var filter = new PayrollRunFilter
            {
                FiscalYearId = query.FiscalYearId,
                Status = query.Status
            };

            var pagedRuns = await _unitOfWork.PayrollRuns.GetPagedByFilterAsync(filter, query.Page, query.PageSize, cancellationToken);

            var runDtos = new List<PayrollRunDto>();
            foreach (var run in pagedRuns.Items)
            {
                var runDto = PayrollRunMapper.ToDto(run, includeSlips: false);
                runDtos.Add(runDto);
            }

            var paginatedResponse = new PaginatedResponse<PayrollRunDto>
            {
                Items = runDtos,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = pagedRuns.TotalCount
            };

            var successResponse = CommonResponse<PaginatedResponse<PayrollRunDto>>.Success(paginatedResponse);
            return successResponse;
        }

        public async Task<CommonResponse<PayrollRunDto>> GetRunByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var run = await _unitOfWork.PayrollRuns.GetByIdWithSlipsAsync(id, cancellationToken);
            if (run == null)
            {
                var notFoundResponse = CommonResponse<PayrollRunDto>.Fail(ResponseCodes.NotFound, "Payroll run with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            var runDto = PayrollRunMapper.ToDto(run, includeSlips: true);
            var successResponse = CommonResponse<PayrollRunDto>.Success(runDto);
            return successResponse;
        }

        // Approval locks the run (P5/P6): slips leave Draft, lines become immutable, and the
        // consumed adjustments were already stamped Applied at generation.
        public async Task<CommonResponse<PayrollRunDto>> ApproveRunAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var run = await _unitOfWork.PayrollRuns.GetByIdWithSlipsAsync(id, cancellationToken);
            if (run == null)
            {
                var notFoundResponse = CommonResponse<PayrollRunDto>.Fail(ResponseCodes.NotFound, "Payroll run with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            if (run.Status != PayrollRunStatus.Draft)
            {
                var wrongStateResponse = CommonResponse<PayrollRunDto>.Fail(ResponseCodes.Conflict, "Only a Draft run can be approved; this one is '" + run.Status + "'.");
                return wrongStateResponse;
            }

            run.Status = PayrollRunStatus.Approved;
            run.ApprovedTs = DateTime.UtcNow;
            run.ApprovedBy = _currentUserService.UserName ?? "system";

            foreach (var slip in run.Slips)
            {
                if (slip.Status == SalarySlipStatus.Draft)
                {
                    slip.Status = SalarySlipStatus.Approved;
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var runDto = PayrollRunMapper.ToDto(run, includeSlips: true);
            var successResponse = CommonResponse<PayrollRunDto>.Success(runDto, "Payroll run approved.");
            return successResponse;
        }

        public async Task<CommonResponse<PayrollRunDto>> MarkPaidAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var run = await _unitOfWork.PayrollRuns.GetByIdWithSlipsAsync(id, cancellationToken);
            if (run == null)
            {
                var notFoundResponse = CommonResponse<PayrollRunDto>.Fail(ResponseCodes.NotFound, "Payroll run with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            if (run.Status != PayrollRunStatus.Approved)
            {
                var wrongStateResponse = CommonResponse<PayrollRunDto>.Fail(ResponseCodes.Conflict, "Only an Approved run can be marked paid; this one is '" + run.Status + "'.");
                return wrongStateResponse;
            }

            run.Status = PayrollRunStatus.Paid;
            run.PaidTs = DateTime.UtcNow;

            foreach (var slip in run.Slips)
            {
                if (slip.Status == SalarySlipStatus.Approved)
                {
                    slip.Status = SalarySlipStatus.Paid;
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var runDto = PayrollRunMapper.ToDto(run, includeSlips: true);
            var successResponse = CommonResponse<PayrollRunDto>.Success(runDto, "Payroll run marked as paid.");
            return successResponse;
        }

        // Cancelling frees the (fiscal year, month) slot and re-pends every adjustment the run
        // consumed (S1). Allowed from Draft, and from Approved while unpaid (the controller
        // gates that path behind a separate elevated permission); never from Paid.
        public async Task<CommonResponse<PayrollRunDto>> CancelRunAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var run = await _unitOfWork.PayrollRuns.GetByIdWithSlipsAsync(id, cancellationToken);
            if (run == null)
            {
                var notFoundResponse = CommonResponse<PayrollRunDto>.Fail(ResponseCodes.NotFound, "Payroll run with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            if (run.Status == PayrollRunStatus.Paid || run.Status == PayrollRunStatus.Cancelled)
            {
                var wrongStateResponse = CommonResponse<PayrollRunDto>.Fail(ResponseCodes.Conflict, "A '" + run.Status + "' run cannot be cancelled.");
                return wrongStateResponse;
            }

            run.Status = PayrollRunStatus.Cancelled;

            var slipIds = new List<Guid>();
            foreach (var slip in run.Slips)
            {
                slipIds.Add(slip.Id);
                slip.Status = SalarySlipStatus.Cancelled;
            }

            var appliedAdjustments = await _unitOfWork.Employees.GetSalaryAdjustmentsAppliedToSlipsAsync(slipIds, cancellationToken);
            foreach (var appliedAdjustment in appliedAdjustments)
            {
                appliedAdjustment.Status = AdjustmentStatus.Pending;
                appliedAdjustment.AppliedSalarySlipId = null;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var runDto = PayrollRunMapper.ToDto(run, includeSlips: true);
            var successResponse = CommonResponse<PayrollRunDto>.Success(runDto, "Payroll run cancelled; its consumed adjustments are Pending again.");
            return successResponse;
        }

        public async Task<CommonResponse<SalarySlipDto>> GetSlipByIdAsync(Guid runId, Guid slipId, CancellationToken cancellationToken = default)
        {
            var slip = await _unitOfWork.PayrollRuns.GetSlipByIdAsync(slipId, cancellationToken);
            if (slip == null || slip.PayrollRunId != runId)
            {
                var notFoundResponse = CommonResponse<SalarySlipDto>.Fail(ResponseCodes.NotFound, "Salary slip was not found on this payroll run.");
                return notFoundResponse;
            }

            var slipDto = PayrollRunMapper.ToSlipDto(slip, includeLines: true);
            var successResponse = CommonResponse<SalarySlipDto>.Success(slipDto);
            return successResponse;
        }

        // Individually cancels one slip (a resigned employee inside an otherwise-valid run,
        // P5) while the run is still unpaid; the slip's adjustments re-pend.
        public async Task<CommonResponse<SalarySlipDto>> CancelSlipAsync(Guid runId, Guid slipId, CancellationToken cancellationToken = default)
        {
            var slip = await _unitOfWork.PayrollRuns.GetSlipByIdAsync(slipId, cancellationToken);
            if (slip == null || slip.PayrollRunId != runId)
            {
                var notFoundResponse = CommonResponse<SalarySlipDto>.Fail(ResponseCodes.NotFound, "Salary slip was not found on this payroll run.");
                return notFoundResponse;
            }

            if (slip.Status == SalarySlipStatus.Paid || slip.Status == SalarySlipStatus.Cancelled)
            {
                var wrongStateResponse = CommonResponse<SalarySlipDto>.Fail(ResponseCodes.Conflict, "A '" + slip.Status + "' slip cannot be cancelled.");
                return wrongStateResponse;
            }

            slip.Status = SalarySlipStatus.Cancelled;

            var appliedAdjustments = await _unitOfWork.Employees.GetSalaryAdjustmentsAppliedToSlipsAsync(new List<Guid> { slipId }, cancellationToken);
            foreach (var appliedAdjustment in appliedAdjustments)
            {
                appliedAdjustment.Status = AdjustmentStatus.Pending;
                appliedAdjustment.AppliedSalarySlipId = null;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var slipDto = PayrollRunMapper.ToSlipDto(slip, includeLines: true);
            var successResponse = CommonResponse<SalarySlipDto>.Success(slipDto, "Salary slip cancelled.");
            return successResponse;
        }

        // 2026-07-22: approve one slip independently of the whole run -- the finer-grained
        // counterpart to ApproveRunAsync's bulk lock. Deliberately does NOT touch the run's own
        // status or any other slip; the bulk "Approve Run" action remains the way to lock
        // everything at once. Locks the slip's lines the same way approving the run does (every
        // Add/Update/Remove Slip Line call already rejects a non-Draft slip).
        public async Task<CommonResponse<SalarySlipDto>> ApproveSlipAsync(Guid runId, Guid slipId, CancellationToken cancellationToken = default)
        {
            var slip = await _unitOfWork.PayrollRuns.GetSlipByIdAsync(slipId, cancellationToken);
            if (slip == null || slip.PayrollRunId != runId)
            {
                var notFoundResponse = CommonResponse<SalarySlipDto>.Fail(ResponseCodes.NotFound, "Salary slip was not found on this payroll run.");
                return notFoundResponse;
            }

            if (slip.Status != SalarySlipStatus.Draft)
            {
                var wrongStateResponse = CommonResponse<SalarySlipDto>.Fail(ResponseCodes.Conflict, "Only a Draft slip can be approved; this one is '" + slip.Status + "'.");
                return wrongStateResponse;
            }

            slip.Status = SalarySlipStatus.Approved;

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var slipDto = PayrollRunMapper.ToSlipDto(slip, includeLines: true);
            var successResponse = CommonResponse<SalarySlipDto>.Success(slipDto, "Salary slip approved.");
            return successResponse;
        }

        // 2026-07-22: the deliberate, admin-triggered counterpart to CancelSlipAsync -- fully
        // rebuilds ONE slip from the current compensation plan/tax configuration (same pipeline
        // CreateRunAsync/RefreshRunAsync use), un-cancelling it in the process. Works on a
        // Cancelled slip (the primary case: "actually, un-cancel and rebuild this one") or an
        // already-Draft slip (equivalent to refreshing just this one employee). Deliberately does
        // NOT run for Approved/Paid slips, and deliberately does NOT happen automatically as part
        // of a whole-run Refresh -- RefreshRunAsync intentionally leaves an individually-cancelled
        // slip cancelled (it may represent a real leaver), so reviving it is a separate, explicit
        // per-slip action an admin opts into here, not a side effect of refreshing everyone else.
        public async Task<CommonResponse<SalarySlipDto>> RegenerateSlipAsync(Guid runId, Guid slipId, CancellationToken cancellationToken = default)
        {
            var run = await _unitOfWork.PayrollRuns.GetByIdAsync(runId, cancellationToken);
            var slip = await _unitOfWork.PayrollRuns.GetSlipByIdAsync(slipId, cancellationToken);
            if (run == null || slip == null || slip.PayrollRunId != runId)
            {
                var notFoundResponse = CommonResponse<SalarySlipDto>.Fail(ResponseCodes.NotFound, "Salary slip was not found on this payroll run.");
                return notFoundResponse;
            }

            if (slip.Status != SalarySlipStatus.Cancelled && slip.Status != SalarySlipStatus.Draft)
            {
                var wrongStateResponse = CommonResponse<SalarySlipDto>.Fail(ResponseCodes.Conflict, "Only a Cancelled or Draft slip can be regenerated; this one is '" + slip.Status + "'.");
                return wrongStateResponse;
            }

            var fiscalYear = await _unitOfWork.FiscalYears.GetByIdAsync(run.FiscalYearId, cancellationToken);
            if (fiscalYear == null)
            {
                var noFiscalYearResponse = CommonResponse<SalarySlipDto>.Fail(ResponseCodes.NotFound, "Fiscal year for this run was not found.");
                return noFiscalYearResponse;
            }

            // Deliberately the SAME eligible-employee source CreateRunAsync/RefreshRunAsync use --
            // if the employee is no longer payroll-eligible (e.g. the leaver this slip was
            // cancelled for), they won't be found here, and regeneration correctly refuses rather
            // than silently reviving pay for someone who shouldn't be paid.
            var employees = await _unitOfWork.Employees.GetPayrollEligibleEmployeesAsync(cancellationToken);
            Employee employee = null;
            foreach (var candidate in employees)
            {
                if (candidate.Id == slip.EmployeeId)
                {
                    employee = candidate;
                }
            }

            if (employee == null)
            {
                var notEligibleResponse = CommonResponse<SalarySlipDto>.Fail(ResponseCodes.ValidationError, "This employee is not currently payroll-eligible -- can't regenerate their slip.");
                return notEligibleResponse;
            }

            var salary = ResolveSalaryRevisionForMonth(employee, fiscalYear, run.MonthIndex);
            if (salary == null)
            {
                var noSalaryResponse = CommonResponse<SalarySlipDto>.Fail(ResponseCodes.ValidationError, "No compensation-plan revision is effective for this month -- can't regenerate.");
                return noSalaryResponse;
            }

            var taxSlabs = await _unitOfWork.FiscalYears.GetTaxSlabsAsync(fiscalYear.Id, salary.AssessmentType, cancellationToken);
            if (taxSlabs.Count == 0)
            {
                var noSlabsResponse = CommonResponse<SalarySlipDto>.Fail(ResponseCodes.ValidationError, "Fiscal year '" + fiscalYear.Code + "' has no tax slabs configured for assessment type '" + salary.AssessmentType + "'.");
                return noSlabsResponse;
            }

            var insuranceTypeCaps = await BuildInsuranceTypeCapsAsync(cancellationToken);
            var payrollLabels = await LoadPayrollLabelMapAsync(cancellationToken);
            var calculationConfigByCode = await LoadSalaryLineCalculationConfigAsync(cancellationToken);

            var components = salary.Components.ToList();
            var deductions = salary.Deductions.ToList();

            var taxCalculation = TaxCalculator.CalculateFromSalary(
                components,
                deductions,
                salary.InsurancePremiums.ToList(),
                insuranceTypeCaps,
                fiscalYear.RetirementExemptionCapAmount,
                taxSlabs,
                null,
                calculationConfigByCode);

            var months = MonthlyBreakdownCalculator.Build(components, deductions, salary.EffectiveFromDate, fiscalYear, taxCalculation.AnnualTax, payrollLabels, calculationConfigByCode);
            var monthRow = months[run.MonthIndex - 1];

            var approvedLoans = await _unitOfWork.Employees.GetApprovedLoansByEmployeeIdsAsync(new List<Guid> { employee.Id }, cancellationToken);

            var periodPendingAdjustments = await _unitOfWork.Employees.GetPendingSalaryAdjustmentsForPeriodAsync(fiscalYear.Id, run.MonthIndex, cancellationToken);
            var employeeAdjustments = new List<SalaryAdjustment>();
            foreach (var adjustment in periodPendingAdjustments)
            {
                if (adjustment.EmployeeId == employee.Id)
                {
                    employeeAdjustments.Add(adjustment);
                }
            }

            slip.Status = SalarySlipStatus.Draft;
            RebuildSlip(slip, salary, monthRow, components, approvedLoans, employeeAdjustments, payrollLabels);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var slipDto = PayrollRunMapper.ToSlipDto(slip, includeLines: true);
            var successResponse = CommonResponse<SalarySlipDto>.Success(slipDto, "Salary slip regenerated from the current configuration.");
            return successResponse;
        }

        public async Task<CommonResponse<SalarySlipDto>> AddSlipLineAsync(Guid runId, Guid slipId, SalarySlipLineInput command, CancellationToken cancellationToken = default)
        {
            var validationResult = _lineInputValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<SalarySlipDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var run = await _unitOfWork.PayrollRuns.GetByIdAsync(runId, cancellationToken);
            var slip = await _unitOfWork.PayrollRuns.GetSlipByIdAsync(slipId, cancellationToken);
            if (run == null || slip == null || slip.PayrollRunId != runId)
            {
                var notFoundResponse = CommonResponse<SalarySlipDto>.Fail(ResponseCodes.NotFound, "Salary slip was not found on this payroll run.");
                return notFoundResponse;
            }

            if (slip.Status != SalarySlipStatus.Draft)
            {
                var lockedResponse = CommonResponse<SalarySlipDto>.Fail(ResponseCodes.Conflict, "Only Draft slips can be edited; this one is '" + slip.Status + "'.");
                return lockedResponse;
            }

            var componentCode = command.ComponentCode?.Trim();
            var salary = await _unitOfWork.Employees.GetSalaryWithLineItemsAsync(slip.EmployeeSalaryId, cancellationToken);
            var syncedToStructure = false;

            // 2026-07-22: a manual Earning/Deduction line with a real catalog code becomes a
            // permanent part of the employee's compensation plan too, not just this one slip --
            // adds to an existing component/deduction's Value, or inserts a new one (Fixed,
            // frequency per ApplyDeltaToSalaryStructureAsync's ResolveFrequency call) if the code
            // isn't already on this revision. Blocking: reject the whole add if the code is
            // unknown, mis-catalogued (see SalaryLineCalculationHelper.ValidateCalculateType), or
            // locked to a catalog percentage (see SalaryLineCalculationHelper.ValidatePercentageLock).
            if (salary != null && !string.IsNullOrWhiteSpace(componentCode) && (command.LineType == SalaryLineType.Earning || command.LineType == SalaryLineType.Deduction))
            {
                var structureSyncError = await ApplyDeltaToSalaryStructureAsync(salary, command.LineType, componentCode, command.Amount, allowInsert: true, cancellationToken);
                if (structureSyncError != null)
                {
                    var structureSyncFailureResponse = CommonResponse<SalarySlipDto>.Fail(ResponseCodes.ValidationError, structureSyncError);
                    return structureSyncFailureResponse;
                }

                syncedToStructure = true;
            }

            // Once a line's amount is backed by the real structure, it must NOT also survive as
            // a Manual line across a future refresh -- RebuildSlip removes every non-Manual line
            // and regenerates Salary Structure lines fresh from the (now-updated) components/
            // deductions, so "Manual" here would double the amount the moment this same slip is
            // rebuilt (verified: this employee's own EffectiveFromDate falls in this exact fiscal
            // month, so a synced line's OneTime placement lands right back on this same slip).
            // Marking it SalaryStructure instead means the next refresh correctly replaces it
            // with the one true regenerated line instead of keeping both.
            var line = new SalarySlipLine
            {
                SalarySlipId = slipId,
                LineType = command.LineType,
                Source = syncedToStructure ? SalaryLineSource.SalaryStructure : SalaryLineSource.Manual,
                ComponentCode = componentCode,
                Description = command.Description.Trim(),
                Amount = command.Amount,
                SalarySlip = slip
            };

            slip.Lines.Add(line);
            await _unitOfWork.PayrollRuns.AddSlipLineAsync(line, cancellationToken);

            await RecalculateSlipTaxAsync(slip, salary, run.FiscalYearId, cancellationToken);
            RecomputeSlipTotals(slip);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var slipDto = PayrollRunMapper.ToSlipDto(slip, includeLines: true);
            var successResponse = CommonResponse<SalarySlipDto>.Success(slipDto, "Slip line added successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<SalarySlipDto>> UpdateSlipLineAsync(Guid runId, Guid slipId, Guid lineId, UpdateSalarySlipLineCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _updateLineValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<SalarySlipDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var run = await _unitOfWork.PayrollRuns.GetByIdAsync(runId, cancellationToken);
            var slip = await _unitOfWork.PayrollRuns.GetSlipByIdAsync(slipId, cancellationToken);
            if (run == null || slip == null || slip.PayrollRunId != runId)
            {
                var notFoundResponse = CommonResponse<SalarySlipDto>.Fail(ResponseCodes.NotFound, "Salary slip was not found on this payroll run.");
                return notFoundResponse;
            }

            if (slip.Status != SalarySlipStatus.Draft)
            {
                var lockedResponse = CommonResponse<SalarySlipDto>.Fail(ResponseCodes.Conflict, "Only Draft slips can be edited; this one is '" + slip.Status + "'.");
                return lockedResponse;
            }

            SalarySlipLine line = null;
            foreach (var slipLine in slip.Lines)
            {
                if (slipLine.Id == lineId)
                {
                    line = slipLine;
                }
            }

            if (line == null)
            {
                var lineNotFoundResponse = CommonResponse<SalarySlipDto>.Fail(ResponseCodes.NotFound, "Line was not found on this slip.");
                return lineNotFoundResponse;
            }

            var salary = await _unitOfWork.Employees.GetSalaryWithLineItemsAsync(slip.EmployeeSalaryId, cancellationToken);

            // 2026-07-22: best-effort mirror of the amount change onto whatever structure
            // component/deduction this manual line originally added to/created (matched by
            // code, since a slip line has no direct FK to the structure row it touched) --
            // never blocks the update itself; a mismatch just leaves the structure untouched.
            if (salary != null && line.Source == SalaryLineSource.Manual && !string.IsNullOrWhiteSpace(line.ComponentCode) && (line.LineType == SalaryLineType.Earning || line.LineType == SalaryLineType.Deduction))
            {
                var delta = command.Amount - line.Amount;
                await ApplyDeltaToSalaryStructureAsync(salary, line.LineType, line.ComponentCode, delta, allowInsert: false, cancellationToken);
            }

            line.Description = command.Description.Trim();
            line.Amount = command.Amount;

            await RecalculateSlipTaxAsync(slip, salary, run.FiscalYearId, cancellationToken);
            RecomputeSlipTotals(slip);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var slipDto = PayrollRunMapper.ToSlipDto(slip, includeLines: true);
            var successResponse = CommonResponse<SalarySlipDto>.Success(slipDto, "Slip line updated successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<SalarySlipDto>> RemoveSlipLineAsync(Guid runId, Guid slipId, Guid lineId, CancellationToken cancellationToken = default)
        {
            var run = await _unitOfWork.PayrollRuns.GetByIdAsync(runId, cancellationToken);
            var slip = await _unitOfWork.PayrollRuns.GetSlipByIdAsync(slipId, cancellationToken);
            if (run == null || slip == null || slip.PayrollRunId != runId)
            {
                var notFoundResponse = CommonResponse<SalarySlipDto>.Fail(ResponseCodes.NotFound, "Salary slip was not found on this payroll run.");
                return notFoundResponse;
            }

            if (slip.Status != SalarySlipStatus.Draft)
            {
                var lockedResponse = CommonResponse<SalarySlipDto>.Fail(ResponseCodes.Conflict, "Only Draft slips can be edited; this one is '" + slip.Status + "'.");
                return lockedResponse;
            }

            SalarySlipLine line = null;
            foreach (var slipLine in slip.Lines)
            {
                if (slipLine.Id == lineId)
                {
                    line = slipLine;
                }
            }

            if (line == null)
            {
                var lineNotFoundResponse = CommonResponse<SalarySlipDto>.Fail(ResponseCodes.NotFound, "Line was not found on this slip.");
                return lineNotFoundResponse;
            }

            // A removed MonthlyAdjustment line releases its source adjustment back to Pending,
            // same rule as the fee side.
            if (line.SalaryAdjustmentId.HasValue)
            {
                var sourceAdjustment = await _unitOfWork.Employees.GetSalaryAdjustmentByIdAsync(line.SalaryAdjustmentId.Value, cancellationToken);
                if (sourceAdjustment != null && sourceAdjustment.Status == AdjustmentStatus.Applied)
                {
                    sourceAdjustment.Status = AdjustmentStatus.Pending;
                    sourceAdjustment.AppliedSalarySlipId = null;
                }
            }

            var salary = await _unitOfWork.Employees.GetSalaryWithLineItemsAsync(slip.EmployeeSalaryId, cancellationToken);

            // 2026-07-22: best-effort reversal of whatever this manual line originally added to
            // the structure (matched by code) -- never blocks the removal itself.
            if (salary != null && line.Source == SalaryLineSource.Manual && !string.IsNullOrWhiteSpace(line.ComponentCode) && (line.LineType == SalaryLineType.Earning || line.LineType == SalaryLineType.Deduction))
            {
                await ApplyDeltaToSalaryStructureAsync(salary, line.LineType, line.ComponentCode, -line.Amount, allowInsert: false, cancellationToken);
            }

            slip.Lines.Remove(line);
            _unitOfWork.PayrollRuns.RemoveSlipLine(line);

            await RecalculateSlipTaxAsync(slip, salary, run.FiscalYearId, cancellationToken);
            RecomputeSlipTotals(slip);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var slipDto = PayrollRunMapper.ToSlipDto(slip, includeLines: true);
            var successResponse = CommonResponse<SalarySlipDto>.Success(slipDto, "Slip line removed successfully.");
            return successResponse;
        }

        // ----- generation internals -----

        private SalarySlip BuildSlip(
            PayrollRun run,
            Employee employee,
            EmployeeSalary salary,
            Application.Payroll.Dtos.MonthlyBreakdownRowDto monthRow,
            IReadOnlyList<EmployeeSalaryComponent> components,
            IReadOnlyList<EmployeeLoan> loans,
            IReadOnlyList<SalaryAdjustment> adjustments,
            string slipNo,
            IReadOnlyDictionary<string, string> labelsByCode)
        {
            var slip = new SalarySlip
            {
                Id = Guid.NewGuid(),
                SlipNo = slipNo,
                PayrollRunId = run.Id,
                EmployeeId = employee.Id,
                EmployeeSalaryId = salary.Id,
                Status = SalarySlipStatus.Draft,
                PeriodStartDate = DateTimeHelper.AsUtcDate(monthRow.PeriodStartDate),
                PeriodEndDate = DateTimeHelper.AsUtcDate(monthRow.PeriodEndDate),
                MonthDays = monthRow.MonthDays,
                PayDays = monthRow.MonthDays,
                PayrollRun = run,
                Employee = employee,
                EmployeeSalary = salary
            };

            PopulateSlipLines(slip, monthRow, components, loans, adjustments, labelsByCode);
            return slip;
        }

        // Refresh path: replaces a Draft slip's generated lines with ones rebuilt from the
        // current configuration, in place -- the slip row (id, slip no) survives, which is what
        // keeps the (payroll_run_id, employee_id) unique index and any external references happy.
        // Manual-source lines are preserved: they are the admin's explicit edits, not derived
        // data, so a refresh has nothing newer to replace them with.
        private void RebuildSlip(
            SalarySlip slip,
            EmployeeSalary salary,
            Application.Payroll.Dtos.MonthlyBreakdownRowDto monthRow,
            IReadOnlyList<EmployeeSalaryComponent> components,
            IReadOnlyList<EmployeeLoan> loans,
            IReadOnlyList<SalaryAdjustment> adjustments,
            IReadOnlyDictionary<string, string> labelsByCode)
        {
            var generatedLines = new List<SalarySlipLine>();
            foreach (var line in slip.Lines)
            {
                if (line.Source != SalaryLineSource.Manual)
                {
                    generatedLines.Add(line);
                }
            }

            foreach (var line in generatedLines)
            {
                slip.Lines.Remove(line);
                _unitOfWork.PayrollRuns.RemoveSlipLine(line);
            }

            slip.EmployeeSalaryId = salary.Id;
            slip.EmployeeSalary = salary;
            slip.PeriodStartDate = DateTimeHelper.AsUtcDate(monthRow.PeriodStartDate);
            slip.PeriodEndDate = DateTimeHelper.AsUtcDate(monthRow.PeriodEndDate);
            slip.MonthDays = monthRow.MonthDays;
            slip.PayDays = monthRow.MonthDays;
            slip.UnpaidLeaveDays = 0m;

            PopulateSlipLines(slip, monthRow, components, loans, adjustments, labelsByCode);
        }

        // Shared by BuildSlip (initial generation) and RebuildSlip (refresh): adds the
        // structure/tax/loan-EMI/adjustment lines to a slip whose PayDays/UnpaidLeaveDays are at
        // their reset values, then recomputes the stored totals. Any Manual lines already on the
        // slip are left alone and simply flow into the recomputed totals -- UNLESS a surviving
        // line shares its ComponentCode with a regenerated Salary Structure line (2026-07-22):
        // that happens when a manual adjustment (a Dashain bonus, overtime, ...) was synced into
        // the structure -- possibly a legacy line from before a Manual-vs-SalaryStructure
        // labeling fix, or any other same-code collision -- and re-adding a second row for the
        // same code would double it on this one slip. In that case the surviving line is updated
        // in place (new amount/label/Source) instead of a duplicate being added.
        private static void PopulateSlipLines(
            SalarySlip slip,
            Application.Payroll.Dtos.MonthlyBreakdownRowDto monthRow,
            IReadOnlyList<EmployeeSalaryComponent> components,
            IReadOnlyList<EmployeeLoan> loans,
            IReadOnlyList<SalaryAdjustment> adjustments,
            IReadOnlyDictionary<string, string> labelsByCode)
        {
            foreach (var incomeLine in monthRow.IncomeLines)
            {
                var existingLine = FindLineByCode(slip.Lines, SalaryLineType.Earning, incomeLine.Code);
                if (existingLine != null)
                {
                    existingLine.Source = SalaryLineSource.SalaryStructure;
                    existingLine.Description = incomeLine.Label;
                    existingLine.Amount = incomeLine.Amount;
                    continue;
                }

                var earningLine = new SalarySlipLine
                {
                    SalarySlipId = slip.Id,
                    LineType = SalaryLineType.Earning,
                    Source = SalaryLineSource.SalaryStructure,
                    ComponentCode = incomeLine.Code,
                    Description = incomeLine.Label,
                    Amount = incomeLine.Amount,
                    SalarySlip = slip
                };
                slip.Lines.Add(earningLine);
            }

            foreach (var deductionLine in monthRow.DeductionLines)
            {
                var existingLine = FindLineByCode(slip.Lines, SalaryLineType.Deduction, deductionLine.Code);
                if (existingLine != null)
                {
                    existingLine.Source = SalaryLineSource.SalaryStructure;
                    existingLine.Description = deductionLine.Label;
                    existingLine.Amount = deductionLine.Amount;
                    continue;
                }

                var structureDeductionLine = new SalarySlipLine
                {
                    SalarySlipId = slip.Id,
                    LineType = SalaryLineType.Deduction,
                    Source = SalaryLineSource.SalaryStructure,
                    ComponentCode = deductionLine.Code,
                    Description = deductionLine.Label,
                    Amount = deductionLine.Amount,
                    SalarySlip = slip
                };
                slip.Lines.Add(structureDeductionLine);
            }

            if (monthRow.MonthTax > 0m)
            {
                var taxLine = new SalarySlipLine
                {
                    SalarySlipId = slip.Id,
                    LineType = SalaryLineType.Tax,
                    Source = SalaryLineSource.TaxCalculator,
                    ComponentCode = "TDS",
                    Description = "TDS (income tax)",
                    Amount = Math.Round(monthRow.MonthTax, 2),
                    SalarySlip = slip
                };
                slip.Lines.Add(taxLine);
            }

            foreach (var loan in loans)
            {
                if (!LoanCalculator.IsDueInPeriod(loan, monthRow.PeriodStartDate))
                {
                    continue;
                }

                var emiLine = new SalarySlipLine
                {
                    SalarySlipId = slip.Id,
                    LineType = SalaryLineType.LoanEmi,
                    Source = SalaryLineSource.LoanSchedule,
                    ComponentCode = loan.LoanTypeCode,
                    EmployeeLoanId = loan.Id,
                    Description = ConfigLabelHelper.Resolve(labelsByCode, loan.LoanTypeCode) + " EMI",
                    Amount = loan.EmiAmount,
                    SalarySlip = slip
                };
                slip.Lines.Add(emiLine);
            }

            var basicPeriodAmount = TaxCalculator.FindBasicPeriodAmount(components);

            foreach (var adjustment in adjustments)
            {
                decimal adjustmentAmount;

                if (adjustment.AdjustmentTypeCode == SalaryAdjustmentTypeCodes.UnpaidLeave)
                {
                    // P4: days x (this month's recurring earnings / month days); also reflected
                    // in the slip's PayDays/UnpaidLeaveDays.
                    var unpaidDays = adjustment.Quantity ?? 0m;
                    var perDayAmount = slip.MonthDays > 0 ? monthRow.MonthGrossIncome / slip.MonthDays : 0m;
                    adjustmentAmount = Math.Round(perDayAmount * unpaidDays, 2);

                    slip.UnpaidLeaveDays += unpaidDays;
                    slip.PayDays = Math.Max(0m, slip.PayDays - unpaidDays);
                }
                else
                {
                    var baseAmount = TaxCalculator.ResolveAmount(adjustment.ValueType, adjustment.Value, basicPeriodAmount);
                    adjustmentAmount = Math.Round(baseAmount * (adjustment.Quantity ?? 1m), 2);
                }

                if (adjustmentAmount <= 0m)
                {
                    adjustment.Status = AdjustmentStatus.Applied;
                    adjustment.AppliedSalarySlipId = slip.Id;
                    continue;
                }

                var adjustmentLine = new SalarySlipLine
                {
                    SalarySlipId = slip.Id,
                    LineType = adjustment.Direction == AdjustmentDirection.Increase ? SalaryLineType.Earning : SalaryLineType.Deduction,
                    Source = SalaryLineSource.MonthlyAdjustment,
                    ComponentCode = adjustment.AdjustmentTypeCode,
                    SalaryAdjustmentId = adjustment.Id,
                    Description = ConfigLabelHelper.Resolve(labelsByCode, adjustment.AdjustmentTypeCode) + (string.IsNullOrWhiteSpace(adjustment.Remarks) ? string.Empty : " - " + adjustment.Remarks),
                    Amount = adjustmentAmount,
                    SalarySlip = slip
                };
                slip.Lines.Add(adjustmentLine);

                adjustment.Status = AdjustmentStatus.Applied;
                adjustment.AppliedSalarySlipId = slip.Id;
            }

            RecomputeSlipTotals(slip);
        }

        // Used by PopulateSlipLines to detect a same-code collision between a surviving line
        // (almost always a Manual one that got synced into the structure) and a freshly
        // regenerated Salary Structure line, so the two merge into one row instead of doubling.
        private static SalarySlipLine FindLineByCode(IEnumerable<SalarySlipLine> lines, SalaryLineType lineType, string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return null;
            }

            foreach (var line in lines)
            {
                if (line.LineType == lineType && line.ComponentCode == code)
                {
                    return line;
                }
            }

            return null;
        }

        // "Effective for the month" = the latest revision whose EffectiveFromDate is on/before
        // the fiscal month's period end (P2) -- a refinement over "always the latest revision".
        private static EmployeeSalary ResolveSalaryRevisionForMonth(Employee employee, FiscalYear fiscalYear, int monthIndex)
        {
            var periodEnd = ResolveFiscalMonthEnd(fiscalYear, monthIndex);

            EmployeeSalary effectiveSalary = null;
            foreach (var salary in employee.Salaries)
            {
                if (salary.EffectiveFromDate.Date > periodEnd)
                {
                    continue;
                }

                if (effectiveSalary == null || salary.EffectiveFromDate > effectiveSalary.EffectiveFromDate)
                {
                    effectiveSalary = salary;
                }
            }

            return effectiveSalary;
        }

        // Same fiscal-month boundary arithmetic as MonthlyBreakdownCalculator (equal Gregorian
        // twelfths of the fiscal year) so the run and the Tax Details view can never disagree.
        private static DateTime ResolveFiscalMonthEnd(FiscalYear fiscalYear, int monthIndex)
        {
            if (monthIndex == 12)
            {
                return fiscalYear.EndDate.Date;
            }

            var totalDays = (fiscalYear.EndDate.Date - fiscalYear.StartDate.Date).Days + 1;
            var periodEnd = fiscalYear.StartDate.Date.AddDays((double)(Math.Round(totalDays * monthIndex / 12m) - 1));
            return periodEnd;
        }

        private static void RecomputeSlipTotals(SalarySlip slip)
        {
            decimal grossEarnings = 0m;
            decimal taxAmount = 0m;
            decimal totalDeductions = 0m;

            foreach (var line in slip.Lines)
            {
                if (line.LineType == SalaryLineType.Earning)
                {
                    grossEarnings += line.Amount;
                    continue;
                }

                if (line.LineType == SalaryLineType.Tax)
                {
                    taxAmount += line.Amount;
                }

                totalDeductions += line.Amount;
            }

            slip.GrossEarnings = grossEarnings;
            slip.TaxAmount = taxAmount;
            slip.TotalDeductions = totalDeductions;
            slip.NetPay = grossEarnings - totalDeductions;
        }

        // 2026-07-22: re-derives this slip's TDS line from the (possibly just-changed) salary
        // structure -- editing a Draft slip's manual lines used to leave the Tax line frozen at
        // its generation-time value, so NetPay silently ignored the added/removed income. Reuses
        // the exact same TaxCalculator pipeline generation/refresh already use, so this can never
        // disagree with them. No-ops (leaves the Tax line as-is) if the salary, fiscal year, or
        // tax slabs can't be resolved -- a slip line edit should never itself start failing
        // because of an unrelated missing config.
        private async Task RecalculateSlipTaxAsync(SalarySlip slip, EmployeeSalary salary, Guid fiscalYearId, CancellationToken cancellationToken)
        {
            if (salary == null)
            {
                return;
            }

            var fiscalYear = await _unitOfWork.FiscalYears.GetByIdAsync(fiscalYearId, cancellationToken);
            if (fiscalYear == null)
            {
                return;
            }

            var taxSlabs = await _unitOfWork.FiscalYears.GetTaxSlabsAsync(fiscalYearId, salary.AssessmentType, cancellationToken);
            if (taxSlabs.Count == 0)
            {
                return;
            }

            var insuranceTypeCaps = await BuildInsuranceTypeCapsAsync(cancellationToken);
            var calculationConfigByCode = await LoadSalaryLineCalculationConfigAsync(cancellationToken);

            var taxCalculation = TaxCalculator.CalculateFromSalary(
                salary.Components.ToList(),
                salary.Deductions.ToList(),
                salary.InsurancePremiums.ToList(),
                insuranceTypeCaps,
                fiscalYear.RetirementExemptionCapAmount,
                taxSlabs,
                null,
                calculationConfigByCode);

            SalarySlipLine taxLine = null;
            foreach (var existingLine in slip.Lines)
            {
                if (existingLine.LineType == SalaryLineType.Tax)
                {
                    taxLine = existingLine;
                }
            }

            if (taxLine != null)
            {
                taxLine.Amount = taxCalculation.MonthlyTax;
            }
            else if (taxCalculation.MonthlyTax > 0m)
            {
                var newTaxLine = new SalarySlipLine
                {
                    SalarySlipId = slip.Id,
                    LineType = SalaryLineType.Tax,
                    Source = SalaryLineSource.TaxCalculator,
                    ComponentCode = "TDS",
                    Description = "TDS (income tax)",
                    Amount = taxCalculation.MonthlyTax,
                    SalarySlip = slip
                };
                slip.Lines.Add(newTaxLine);
                await _unitOfWork.PayrollRuns.AddSlipLineAsync(newTaxLine, cancellationToken);
            }
        }

        // 2026-07-22: upserts a manual Earning/Deduction slip line's amount (or its delta, on
        // update/remove) into the employee's actual compensation plan, by ComponentCode/
        // DeductionCode -- adds to an existing Fixed-amount line, or (allowInsert only) inserts a
        // new Fixed/OneTime one. Returns an error message (Add should reject on it) or null
        // (Update/Remove call this best-effort and ignore the result). A percentage-locked
        // catalog code (see SalaryLineCalculationHelper) can never take a manual cash amount.
        private async Task<string> ApplyDeltaToSalaryStructureAsync(EmployeeSalary salary, SalaryLineType lineType, string code, decimal delta, bool allowInsert, CancellationToken cancellationToken)
        {
            if (delta == 0m)
            {
                return null;
            }

            if (lineType == SalaryLineType.Earning)
            {
                EmployeeSalaryComponent existingComponent = null;
                foreach (var component in salary.Components)
                {
                    if (component.ComponentCode == code)
                    {
                        existingComponent = component;
                    }
                }

                if (existingComponent != null)
                {
                    if (existingComponent.ValueType != AwardValueType.FixedAmount)
                    {
                        return allowInsert ? "'" + code + "' is Percentage-valued on this salary revision -- a manual cash amount can only merge into a Fixed-amount component." : null;
                    }

                    existingComponent.Value = Math.Max(0m, existingComponent.Value + delta);
                    return null;
                }

                if (!allowInsert)
                {
                    return null;
                }

                var catalogOption = await _unitOfWork.Configs.GetByTypeCodeAndCodeAsync(ConfigTypeCodes.SalaryComponentType, code, cancellationToken);
                if (catalogOption == null)
                {
                    return "ComponentCode '" + code + "' is not a known salary component option.";
                }

                var calculateTypeError = SalaryLineCalculationHelper.ValidateCalculateType(catalogOption, SalaryLineCalculateTypes.Addition);
                if (calculateTypeError != null)
                {
                    return calculateTypeError;
                }

                var percentageLockError = SalaryLineCalculationHelper.ValidatePercentageLock(catalogOption, AwardValueType.FixedAmount, delta);
                if (percentageLockError != null)
                {
                    return "'" + code + "' is locked to a catalog percentage and can't take a manual cash amount -- adjust it through the salary revision tools instead.";
                }

                // 2026-07-23: the inserted line's frequency now follows the catalog's own locked
                // FREQUENCY segment when the code carries one (e.g. a recurring allowance merges
                // in as Monthly, not a one-off) -- falls back to OneTime for a code with no rule
                // at all, which is today's exact prior behavior.
                var newComponent = new EmployeeSalaryComponent
                {
                    EmployeeSalaryId = salary.Id,
                    ComponentCode = code,
                    ValueType = AwardValueType.FixedAmount,
                    Value = delta,
                    FrequencyType = SalaryLineCalculationHelper.ResolveFrequency(catalogOption) ?? PayFrequencyType.OneTime,
                    IsTaxable = true,
                    IsRetirementContribution = false,
                    EmployeeSalary = salary
                };
                salary.Components.Add(newComponent);
                await _unitOfWork.Employees.AddSalaryComponentAsync(newComponent, cancellationToken);
                return null;
            }

            EmployeeSalaryDeduction existingDeduction = null;
            foreach (var deduction in salary.Deductions)
            {
                if (deduction.DeductionCode == code)
                {
                    existingDeduction = deduction;
                }
            }

            if (existingDeduction != null)
            {
                if (existingDeduction.ValueType != AwardValueType.FixedAmount)
                {
                    return allowInsert ? "'" + code + "' is Percentage-valued on this salary revision -- a manual cash amount can only merge into a Fixed-amount deduction." : null;
                }

                existingDeduction.Value = Math.Max(0m, existingDeduction.Value + delta);
                return null;
            }

            if (!allowInsert)
            {
                return null;
            }

            var deductionCatalogOption = await _unitOfWork.Configs.GetByTypeCodeAndCodeAsync(ConfigTypeCodes.DeductionType, code, cancellationToken);
            if (deductionCatalogOption == null)
            {
                return "DeductionCode '" + code + "' is not a known deduction option.";
            }

            var deductionCalculateTypeError = SalaryLineCalculationHelper.ValidateCalculateType(deductionCatalogOption, SalaryLineCalculateTypes.Deduction);
            if (deductionCalculateTypeError != null)
            {
                return deductionCalculateTypeError;
            }

            var deductionPercentageLockError = SalaryLineCalculationHelper.ValidatePercentageLock(deductionCatalogOption, AwardValueType.FixedAmount, delta);
            if (deductionPercentageLockError != null)
            {
                return "'" + code + "' is locked to a catalog percentage and can't take a manual cash amount -- adjust it through the salary revision tools instead.";
            }

            var newDeduction = new EmployeeSalaryDeduction
            {
                EmployeeSalaryId = salary.Id,
                DeductionCode = code,
                ValueType = AwardValueType.FixedAmount,
                Value = delta,
                FrequencyType = SalaryLineCalculationHelper.ResolveFrequency(deductionCatalogOption) ?? PayFrequencyType.OneTime,
                IsRetirementContribution = false,
                EmployeeSalary = salary
            };
            salary.Deductions.Add(newDeduction);
            await _unitOfWork.Employees.AddSalaryDeductionAsync(newDeduction, cancellationToken);
            return null;
        }

        // Same Config-catalog read as EmployeeService's tax-calculation path (InsuranceType
        // options carry their Nepal tax-deduction cap in AdditionalValue1).
        private async Task<Dictionary<string, InsuranceCapConfig>> BuildInsuranceTypeCapsAsync(CancellationToken cancellationToken)
        {
            var insuranceTypeConfigs = await _unitOfWork.Configs.GetByTypeCodeAsync(ConfigTypeCodes.InsuranceType, cancellationToken);
            return InsuranceCapHelper.BuildCapMap(insuranceTypeConfigs);
        }

        // Merged label map for every catalog a slip line's code can come from -- salary
        // components, deductions, and salary adjustment types (2026-07-19). Generated slip line
        // Descriptions are written with the human-readable label instead of the raw code.
        private async Task<Dictionary<string, string>> LoadPayrollLabelMapAsync(CancellationToken cancellationToken)
        {
            var labelsByCode = ConfigLabelHelper.BuildLabelMap(await _unitOfWork.Configs.GetByTypeCodeAsync(ConfigTypeCodes.SalaryComponentType, cancellationToken));
            ConfigLabelHelper.MergeLabelMap(labelsByCode, await _unitOfWork.Configs.GetByTypeCodeAsync(ConfigTypeCodes.DeductionType, cancellationToken));
            ConfigLabelHelper.MergeLabelMap(labelsByCode, await _unitOfWork.Configs.GetByTypeCodeAsync(ConfigTypeCodes.SalaryAdjustmentType, cancellationToken));
            return labelsByCode;
        }

        // Same Config-catalog read as EmployeeService's tax-calculation path (2026-07-22, format
        // updated 2026-07-23): every code whose AdditionalValue1 parses as the composite
        // "CALCULATE_TYPE|TYPE|FREQUENCY" rule gets an entry.
        private async Task<Dictionary<string, SalaryLineCalculationConfig>> LoadSalaryLineCalculationConfigAsync(CancellationToken cancellationToken)
        {
            var configByCode = SalaryLineCalculationHelper.BuildConfigMap(await _unitOfWork.Configs.GetByTypeCodeAsync(ConfigTypeCodes.SalaryComponentType, cancellationToken));
            SalaryLineCalculationHelper.MergeConfigMap(configByCode, await _unitOfWork.Configs.GetByTypeCodeAsync(ConfigTypeCodes.DeductionType, cancellationToken));
            return configByCode;
        }

        private static string BuildFullName(string firstName, string middleName, string lastName)
        {
            var nameParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(firstName))
            {
                nameParts.Add(firstName);
            }

            if (!string.IsNullOrWhiteSpace(middleName))
            {
                nameParts.Add(middleName);
            }

            if (!string.IsNullOrWhiteSpace(lastName))
            {
                nameParts.Add(lastName);
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
