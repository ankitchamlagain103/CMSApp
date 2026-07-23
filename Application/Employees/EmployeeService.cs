using Application.Common.Helpers;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Validation;
using Application.DocumentTemplates;
using Application.Employees.Commands;
using Application.Employees.Dtos;
using Application.Employees.Queries;
using Application.Employees.Validators;
using Application.Payroll;
using Application.Payroll.Dtos;
using Domain.Common.Filters;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using FluentValidation.Results;

namespace Application.Employees
{
    public class EmployeeService : IEmployeeService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileStorageService _fileStorage;
        private readonly CreateEmployeeCommandValidator _createValidator;
        private readonly UpdateEmployeeCommandValidator _updateValidator;
        private readonly PromoteToTeacherCommandValidator _promoteValidator;
        private readonly AddEmployeeSalaryCommandValidator _addSalaryValidator;
        private readonly SalaryComponentInputValidator _componentValidator;
        private readonly SalaryDeductionInputValidator _deductionValidator;
        private readonly SalaryLineInputValidator _lineValidator;
        private readonly InsurancePremiumInputValidator _premiumValidator;
        private readonly RequestLoanCommandValidator _requestLoanValidator;
        private readonly CreateSalaryAdjustmentCommandValidator _createSalaryAdjustmentValidator;
        private readonly UpdateSalaryAdjustmentCommandValidator _updateSalaryAdjustmentValidator;
        private readonly CreateBulkSalaryAdjustmentCommandValidator _createBulkSalaryAdjustmentValidator;
        private readonly AddEmployeeQualificationCommandValidator _addQualificationValidator;
        private readonly UploadEmployeeDocumentCommandValidator _uploadDocumentValidator;

        public EmployeeService(
            IUnitOfWork unitOfWork,
            IFileStorageService fileStorage,
            CreateEmployeeCommandValidator createValidator,
            UpdateEmployeeCommandValidator updateValidator,
            PromoteToTeacherCommandValidator promoteValidator,
            AddEmployeeSalaryCommandValidator addSalaryValidator,
            SalaryComponentInputValidator componentValidator,
            SalaryDeductionInputValidator deductionValidator,
            SalaryLineInputValidator lineValidator,
            InsurancePremiumInputValidator premiumValidator,
            RequestLoanCommandValidator requestLoanValidator,
            CreateSalaryAdjustmentCommandValidator createSalaryAdjustmentValidator,
            UpdateSalaryAdjustmentCommandValidator updateSalaryAdjustmentValidator,
            CreateBulkSalaryAdjustmentCommandValidator createBulkSalaryAdjustmentValidator,
            AddEmployeeQualificationCommandValidator addQualificationValidator,
            UploadEmployeeDocumentCommandValidator uploadDocumentValidator)
        {
            _unitOfWork = unitOfWork;
            _fileStorage = fileStorage;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _promoteValidator = promoteValidator;
            _addSalaryValidator = addSalaryValidator;
            _componentValidator = componentValidator;
            _deductionValidator = deductionValidator;
            _lineValidator = lineValidator;
            _premiumValidator = premiumValidator;
            _requestLoanValidator = requestLoanValidator;
            _createSalaryAdjustmentValidator = createSalaryAdjustmentValidator;
            _updateSalaryAdjustmentValidator = updateSalaryAdjustmentValidator;
            _createBulkSalaryAdjustmentValidator = createBulkSalaryAdjustmentValidator;
            _addQualificationValidator = addQualificationValidator;
            _uploadDocumentValidator = uploadDocumentValidator;
        }

        public async Task<CommonResponse<EmployeeDto>> CreateEmployeeAsync(CreateEmployeeCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _createValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<EmployeeDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var employeeCategoryCode = command.EmployeeCategoryCode.Trim();
            var categoryExists = await _unitOfWork.Configs.CodeExistsAsync(ConfigTypeCodes.EmployeeCategory, employeeCategoryCode, cancellationToken);
            if (!categoryExists)
            {
                var invalidCategoryResponse = CommonResponse<EmployeeDto>.Fail(ResponseCodes.ValidationError, "EmployeeCategoryCode '" + employeeCategoryCode + "' is not a known employee category option.");
                return invalidCategoryResponse;
            }

            var jobPositionCode = command.JobPositionCode.Trim();
            var positionExists = await _unitOfWork.Configs.CodeExistsAsync(ConfigTypeCodes.JobPosition, jobPositionCode, cancellationToken);
            if (!positionExists)
            {
                var invalidPositionResponse = CommonResponse<EmployeeDto>.Fail(ResponseCodes.ValidationError, "JobPositionCode '" + jobPositionCode + "' is not a known job position option.");
                return invalidPositionResponse;
            }

            var trimmedEmployeeCode = command.EmployeeCode?.Trim();
            if (string.IsNullOrWhiteSpace(trimmedEmployeeCode))
            {
                var employeeCodePrefix = "EMP" + DateTime.UtcNow.Year;
                var existingEmployeeCodes = await _unitOfWork.Employees.GetEmployeeCodesByPrefixAsync(employeeCodePrefix, cancellationToken);
                trimmedEmployeeCode = NumberSequenceHelper.Next(employeeCodePrefix, existingEmployeeCodes, 3);
            }
            else
            {
                var employeeCodeExists = await _unitOfWork.Employees.EmployeeCodeExistsAsync(trimmedEmployeeCode, cancellationToken);
                if (employeeCodeExists)
                {
                    var conflictResponse = CommonResponse<EmployeeDto>.Fail(ResponseCodes.Conflict, "Employee code '" + trimmedEmployeeCode + "' is already in use (possibly by a soft-deleted employee).");
                    return conflictResponse;
                }
            }

            var employee = new Employee
            {
                EmployeeCode = trimmedEmployeeCode,
                FirstName = command.FirstName.Trim(),
                MiddleName = command.MiddleName?.Trim(),
                LastName = command.LastName.Trim(),
                Gender = command.Gender,
                DateOfBirth = command.DateOfBirth,
                Email = command.Email?.Trim(),
                Phone = command.Phone?.Trim(),
                JoinDate = command.JoinDate,
                EmployeeCategoryCode = employeeCategoryCode,
                JobPositionCode = jobPositionCode,
                EmploymentStatus = EmploymentStatus.Active,
                BankName = command.BankName?.Trim(),
                BankAccountNumber = command.BankAccountNumber?.Trim(),
                PaymentMode = command.PaymentMode,
                PanNumber = command.PanNumber?.Trim(),
                ProvidentFundNumber = command.ProvidentFundNumber?.Trim(),
                SsfNumber = command.SsfNumber?.Trim(),
                CitNumber = command.CitNumber?.Trim(),
                GratuityNumber = command.GratuityNumber?.Trim()
            };

            await _unitOfWork.Employees.AddAsync(employee, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var employeeDto = EmployeeMapper.ToDto(employee);
            var successResponse = CommonResponse<EmployeeDto>.Success(employeeDto, "Employee created successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<EmployeeDto>> GetEmployeeByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var employee = await _unitOfWork.Employees.GetByIdWithTeacherAsync(id, cancellationToken);
            if (employee == null)
            {
                var notFoundResponse = CommonResponse<EmployeeDto>.Fail(ResponseCodes.NotFound, "Employee with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            var employeeDto = EmployeeMapper.ToDto(employee);
            var successResponse = CommonResponse<EmployeeDto>.Success(employeeDto);
            return successResponse;
        }

        public async Task<CommonResponse<PaginatedResponse<EmployeeDto>>> GetEmployeesAsync(GetEmployeesQuery query, CancellationToken cancellationToken = default)
        {
            var filter = new EmployeeFilter
            {
                Search = query.Search,
                Phone = query.Phone,
                EmployeeCategoryCode = query.EmployeeCategoryCode,
                JobPositionCode = query.JobPositionCode,
                EmploymentStatus = query.EmploymentStatus,
                Gender = query.Gender,
                DateField = query.DateField,
                FromDate = query.FromDate,
                ToDate = query.ToDate
            };

            var pagedEmployees = await _unitOfWork.Employees.GetPagedByFilterAsync(filter, query.Page, query.PageSize, cancellationToken);

            var employeeDtos = new List<EmployeeDto>();
            foreach (var employee in pagedEmployees.Items)
            {
                var employeeDto = EmployeeMapper.ToDto(employee);
                employeeDtos.Add(employeeDto);
            }

            var paginatedResponse = new PaginatedResponse<EmployeeDto>
            {
                Items = employeeDtos,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = pagedEmployees.TotalCount
            };

            var successResponse = CommonResponse<PaginatedResponse<EmployeeDto>>.Success(paginatedResponse);
            return successResponse;
        }

        public async Task<CommonResponse<EmployeeDto>> UpdateEmployeeAsync(Guid id, UpdateEmployeeCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _updateValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<EmployeeDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var employee = await _unitOfWork.Employees.GetByIdWithTeacherAsync(id, cancellationToken);
            if (employee == null)
            {
                var notFoundResponse = CommonResponse<EmployeeDto>.Fail(ResponseCodes.NotFound, "Employee with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            var employeeCategoryCode = command.EmployeeCategoryCode.Trim();
            var categoryExists = await _unitOfWork.Configs.CodeExistsAsync(ConfigTypeCodes.EmployeeCategory, employeeCategoryCode, cancellationToken);
            if (!categoryExists)
            {
                var invalidCategoryResponse = CommonResponse<EmployeeDto>.Fail(ResponseCodes.ValidationError, "EmployeeCategoryCode '" + employeeCategoryCode + "' is not a known employee category option.");
                return invalidCategoryResponse;
            }

            var jobPositionCode = command.JobPositionCode.Trim();
            var positionExists = await _unitOfWork.Configs.CodeExistsAsync(ConfigTypeCodes.JobPosition, jobPositionCode, cancellationToken);
            if (!positionExists)
            {
                var invalidPositionResponse = CommonResponse<EmployeeDto>.Fail(ResponseCodes.ValidationError, "JobPositionCode '" + jobPositionCode + "' is not a known job position option.");
                return invalidPositionResponse;
            }

            employee.FirstName = command.FirstName.Trim();
            employee.MiddleName = command.MiddleName?.Trim();
            employee.LastName = command.LastName.Trim();
            employee.Gender = command.Gender;
            employee.DateOfBirth = command.DateOfBirth;
            employee.Email = command.Email?.Trim();
            employee.Phone = command.Phone?.Trim();
            employee.JoinDate = command.JoinDate;
            employee.EmployeeCategoryCode = employeeCategoryCode;
            employee.JobPositionCode = jobPositionCode;
            employee.EmploymentStatus = command.EmploymentStatus;
            employee.BankName = command.BankName?.Trim();
            employee.BankAccountNumber = command.BankAccountNumber?.Trim();
            employee.PaymentMode = command.PaymentMode;
            employee.PanNumber = command.PanNumber?.Trim();
            employee.ProvidentFundNumber = command.ProvidentFundNumber?.Trim();
            employee.SsfNumber = command.SsfNumber?.Trim();
            employee.CitNumber = command.CitNumber?.Trim();
            employee.GratuityNumber = command.GratuityNumber?.Trim();

            _unitOfWork.Employees.Update(employee);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var employeeDto = EmployeeMapper.ToDto(employee);
            var successResponse = CommonResponse<EmployeeDto>.Success(employeeDto, "Employee updated successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<bool>> DeleteEmployeeAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var employee = await _unitOfWork.Employees.GetByIdWithTeacherAsync(id, cancellationToken);
            if (employee == null)
            {
                var notFoundResponse = CommonResponse<bool>.Fail(ResponseCodes.NotFound, "Employee with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            if (employee.Teacher != null)
            {
                var hasAssignments = await _unitOfWork.Teachers.HasAssignmentsAsync(employee.Id, cancellationToken);
                if (hasAssignments)
                {
                    var assignmentsConflictResponse = CommonResponse<bool>.Fail(ResponseCodes.Conflict, "This employee's teacher profile still has class assignments. Remove them first.");
                    return assignmentsConflictResponse;
                }
            }

            var hasSalaries = await _unitOfWork.Employees.HasSalariesAsync(id, cancellationToken);
            if (hasSalaries)
            {
                var salaryConflictResponse = CommonResponse<bool>.Fail(ResponseCodes.Conflict, "This employee still has salary records. Remove them first.");
                return salaryConflictResponse;
            }

            _unitOfWork.Employees.Remove(employee);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var successResponse = CommonResponse<bool>.Success(true, "Employee deleted successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<TeacherProfileDto>> PromoteToTeacherAsync(Guid employeeId, PromoteToTeacherCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _promoteValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<TeacherProfileDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var employee = await _unitOfWork.Employees.GetByIdWithTeacherAsync(employeeId, cancellationToken);
            if (employee == null)
            {
                var notFoundResponse = CommonResponse<TeacherProfileDto>.Fail(ResponseCodes.NotFound, "Employee with id '" + employeeId + "' was not found.");
                return notFoundResponse;
            }

            if (employee.Teacher != null)
            {
                var conflictResponse = CommonResponse<TeacherProfileDto>.Fail(ResponseCodes.Conflict, "This employee already has a teacher profile.");
                return conflictResponse;
            }

            if (employee.EmployeeCategoryCode != EmployeeCategoryCodes.Academic
                || (employee.JobPositionCode != JobPositionCodes.Teacher
                    && employee.JobPositionCode != JobPositionCodes.Principal
                    && employee.JobPositionCode != JobPositionCodes.VicePrincipal))
            {
                var ineligibleResponse = CommonResponse<TeacherProfileDto>.Fail(ResponseCodes.ValidationError, "Only Academic-category employees in a Teacher/Principal/Vice Principal position can have a teacher profile.");
                return ineligibleResponse;
            }

            var teacher = new Teacher
            {
                Id = employee.Id,
                TeachingLicenseNo = command.TeachingLicenseNo?.Trim(),
                ExperienceYears = command.ExperienceYears,
                Specialization = command.Specialization?.Trim(),
                Employee = employee
            };

            await _unitOfWork.Teachers.AddAsync(teacher, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var teacherProfileDto = EmployeeMapper.ToTeacherProfileDto(teacher);
            var successResponse = CommonResponse<TeacherProfileDto>.Success(teacherProfileDto, "Teacher profile added successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<EmployeeSalaryDto>> AddSalaryAsync(Guid employeeId, AddEmployeeSalaryCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _addSalaryValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<EmployeeSalaryDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var employee = await _unitOfWork.Employees.GetByIdAsync(employeeId, cancellationToken);
            if (employee == null)
            {
                var notFoundResponse = CommonResponse<EmployeeSalaryDto>.Fail(ResponseCodes.NotFound, "Employee with id '" + employeeId + "' was not found.");
                return notFoundResponse;
            }

            var alreadyExists = await _unitOfWork.Employees.SalaryExistsForDateAsync(employeeId, command.EffectiveFromDate, cancellationToken);
            if (alreadyExists)
            {
                var conflictResponse = CommonResponse<EmployeeSalaryDto>.Fail(ResponseCodes.Conflict, "A salary revision already exists for this employee effective on that date (possibly soft-deleted).");
                return conflictResponse;
            }

            var codeValidationError = await ValidateLineItemCodesAsync(command.Components, command.Deductions, command.InsurancePremiums, cancellationToken);
            if (codeValidationError != null)
            {
                var invalidCodeResponse = CommonResponse<EmployeeSalaryDto>.Fail(ResponseCodes.ValidationError, codeValidationError);
                return invalidCodeResponse;
            }

            var salary = new EmployeeSalary
            {
                EmployeeId = employeeId,
                EffectiveFromDate = command.EffectiveFromDate,
                AssessmentType = command.AssessmentType,
                Employee = employee
            };

            foreach (var componentInput in command.Components)
            {
                salary.Components.Add(new EmployeeSalaryComponent
                {
                    ComponentCode = componentInput.ComponentCode.Trim(),
                    ValueType = componentInput.ValueType,
                    Value = componentInput.Value,
                    FrequencyType = componentInput.FrequencyType,
                    IsTaxable = componentInput.IsTaxable,
                    IsRetirementContribution = componentInput.IsRetirementContribution,
                    EmployeeSalary = salary
                });
            }

            foreach (var deductionInput in command.Deductions)
            {
                salary.Deductions.Add(new EmployeeSalaryDeduction
                {
                    DeductionCode = deductionInput.DeductionCode.Trim(),
                    ValueType = deductionInput.ValueType,
                    Value = deductionInput.Value,
                    FrequencyType = deductionInput.FrequencyType,
                    IsRetirementContribution = deductionInput.IsRetirementContribution,
                    EmployeeSalary = salary
                });
            }

            foreach (var premiumInput in command.InsurancePremiums)
            {
                salary.InsurancePremiums.Add(new EmployeeInsurancePremium
                {
                    InsuranceTypeCode = premiumInput.InsuranceTypeCode.Trim(),
                    AnnualPremiumAmount = premiumInput.AnnualPremiumAmount,
                    EmployeeSalary = salary
                });
            }

            await _unitOfWork.Employees.AddSalaryAsync(salary, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var compensationLabels = await LoadCompensationLabelMapAsync(cancellationToken);
            var salaryDto = EmployeeMapper.ToSalaryDto(salary, compensationLabels);
            var successResponse = CommonResponse<EmployeeSalaryDto>.Success(salaryDto, "Salary revision added successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<List<EmployeeSalaryDto>>> GetSalaryHistoryAsync(Guid employeeId, CancellationToken cancellationToken = default)
        {
            var employee = await _unitOfWork.Employees.GetByIdAsync(employeeId, cancellationToken);
            if (employee == null)
            {
                var notFoundResponse = CommonResponse<List<EmployeeSalaryDto>>.Fail(ResponseCodes.NotFound, "Employee with id '" + employeeId + "' was not found.");
                return notFoundResponse;
            }

            var salaries = await _unitOfWork.Employees.GetSalaryHistoryAsync(employeeId, cancellationToken);
            var compensationLabels = await LoadCompensationLabelMapAsync(cancellationToken);

            var salaryDtos = new List<EmployeeSalaryDto>();
            foreach (var salary in salaries)
            {
                var fullSalary = await _unitOfWork.Employees.GetSalaryWithLineItemsAsync(salary.Id, cancellationToken);
                var salaryDto = EmployeeMapper.ToSalaryDto(fullSalary, compensationLabels);
                salaryDtos.Add(salaryDto);
            }

            var successResponse = CommonResponse<List<EmployeeSalaryDto>>.Success(salaryDtos);
            return successResponse;
        }

        public async Task<CommonResponse<EmployeeTaxCalculationDto>> GetCurrentSalaryTaxCalculationAsync(Guid employeeId, Guid? fiscalYearId, CancellationToken cancellationToken = default)
        {
            var employee = await _unitOfWork.Employees.GetByIdAsync(employeeId, cancellationToken);
            if (employee == null)
            {
                var notFoundResponse = CommonResponse<EmployeeTaxCalculationDto>.Fail(ResponseCodes.NotFound, "Employee with id '" + employeeId + "' was not found.");
                return notFoundResponse;
            }

            var currentSalary = await _unitOfWork.Employees.GetCurrentSalaryAsync(employeeId, cancellationToken);
            if (currentSalary == null)
            {
                var noSalaryResponse = CommonResponse<EmployeeTaxCalculationDto>.Fail(ResponseCodes.NotFound, "This employee has no salary on record yet.");
                return noSalaryResponse;
            }

            var fiscalYear = fiscalYearId.HasValue
                ? await _unitOfWork.FiscalYears.GetByIdAsync(fiscalYearId.Value, cancellationToken)
                : await _unitOfWork.FiscalYears.GetCurrentYearAsync(cancellationToken);
            if (fiscalYear == null)
            {
                var noFiscalYearResponse = CommonResponse<EmployeeTaxCalculationDto>.Fail(ResponseCodes.NotFound, fiscalYearId.HasValue ? "Fiscal year with id '" + fiscalYearId.Value + "' was not found." : "No fiscal year is marked as current.");
                return noFiscalYearResponse;
            }

            var taxSlabs = await _unitOfWork.FiscalYears.GetTaxSlabsAsync(fiscalYear.Id, currentSalary.AssessmentType, cancellationToken);
            if (taxSlabs.Count == 0)
            {
                var noSlabsResponse = CommonResponse<EmployeeTaxCalculationDto>.Fail(ResponseCodes.ValidationError, "Fiscal year '" + fiscalYear.Code + "' has no tax slabs configured for assessment type '" + currentSalary.AssessmentType + "'.");
                return noSlabsResponse;
            }

            var insuranceTypeCaps = await BuildInsuranceTypeCapsAsync(cancellationToken);
            var compensationLabels = await LoadCompensationLabelMapAsync(cancellationToken);
            var calculationConfigByCode = await LoadSalaryLineCalculationConfigAsync(cancellationToken);

            var taxCalculation = TaxCalculator.CalculateFromSalary(
                currentSalary.Components.ToList(),
                currentSalary.Deductions.ToList(),
                currentSalary.InsurancePremiums.ToList(),
                insuranceTypeCaps,
                fiscalYear.RetirementExemptionCapAmount,
                taxSlabs,
                compensationLabels,
                calculationConfigByCode);

            var grossMonthly = Math.Round(taxCalculation.GrossAnnualIncome / 12m, 2);
            var netMonthly = Math.Round(taxCalculation.NetAnnualIncome / 12m, 2);

            var taxCalculationDto = new EmployeeTaxCalculationDto
            {
                EmployeeId = employeeId,
                SalaryId = currentSalary.Id,
                FiscalYearId = fiscalYear.Id,
                FiscalYearCode = fiscalYear.Code,
                GrossMonthly = grossMonthly,
                TaxCalculation = taxCalculation,
                NetMonthly = netMonthly
            };

            var successResponse = CommonResponse<EmployeeTaxCalculationDto>.Success(taxCalculationDto);
            return successResponse;
        }

        // Composes on top of GetCurrentSalaryTaxCalculationAsync (same pattern GetPayslipPreviewAsync
        // already uses) rather than repeating its not-found/no-slabs checks -- the current salary and
        // fiscal year re-fetches below are guaranteed non-null once that call has already succeeded.
        public async Task<CommonResponse<EmployeeMonthlyTaxBreakdownDto>> GetMonthlySalaryTaxCalculationAsync(Guid employeeId, Guid? fiscalYearId, CancellationToken cancellationToken = default)
        {
            var taxCalculationResponse = await GetCurrentSalaryTaxCalculationAsync(employeeId, fiscalYearId, cancellationToken);
            if (taxCalculationResponse.Data == null)
            {
                var failureResponse = CommonResponse<EmployeeMonthlyTaxBreakdownDto>.Fail(taxCalculationResponse.ResponseCode, taxCalculationResponse.ResponseMessage);
                return failureResponse;
            }

            var taxCalculationDto = taxCalculationResponse.Data;

            var currentSalary = await _unitOfWork.Employees.GetCurrentSalaryAsync(employeeId, cancellationToken);
            var fiscalYear = await _unitOfWork.FiscalYears.GetByIdAsync(taxCalculationDto.FiscalYearId, cancellationToken);

            var payrollLabels = await LoadCompensationLabelMapAsync(cancellationToken);
            var calculationConfigByCode = await LoadSalaryLineCalculationConfigAsync(cancellationToken);

            var months = MonthlyBreakdownCalculator.Build(
                currentSalary.Components.ToList(),
                currentSalary.Deductions.ToList(),
                currentSalary.EffectiveFromDate,
                fiscalYear,
                taxCalculationDto.TaxCalculation.AnnualTax,
                payrollLabels,
                calculationConfigByCode);

            var monthlyBreakdownDto = new EmployeeMonthlyTaxBreakdownDto
            {
                EmployeeId = employeeId,
                SalaryId = taxCalculationDto.SalaryId,
                FiscalYearId = taxCalculationDto.FiscalYearId,
                FiscalYearCode = taxCalculationDto.FiscalYearCode,
                TaxCalculation = taxCalculationDto.TaxCalculation,
                Months = months
            };

            var successResponse = CommonResponse<EmployeeMonthlyTaxBreakdownDto>.Success(monthlyBreakdownDto);
            return successResponse;
        }

        // Single composite response for the Investment & Tax Planning tab: income lines,
        // retirement-fund a/b/c breakdown, insurance lines, assessment type, and the annual tax
        // calculation -- one call instead of the tab separately fetching salary history, the
        // fiscal year (for its RetirementExemptionCapAmount), and the tax calculation, then
        // recomputing the a/b/c figures itself. Composes on GetCurrentSalaryTaxCalculationAsync
        // (same pattern GetMonthlySalaryTaxCalculationAsync already uses) so this can never
        // disagree with GET .../salaries/tax-calculation for the same employee/fiscal year.
        public async Task<CommonResponse<TaxPlanningDto>> GetTaxPlanningAsync(Guid employeeId, Guid? fiscalYearId, CancellationToken cancellationToken = default)
        {
            var taxCalculationResponse = await GetCurrentSalaryTaxCalculationAsync(employeeId, fiscalYearId, cancellationToken);
            if (taxCalculationResponse.Data == null)
            {
                var failureResponse = CommonResponse<TaxPlanningDto>.Fail(taxCalculationResponse.ResponseCode, taxCalculationResponse.ResponseMessage);
                return failureResponse;
            }

            var taxCalculationDto = taxCalculationResponse.Data;

            var currentSalary = await _unitOfWork.Employees.GetCurrentSalaryAsync(employeeId, cancellationToken);
            var fiscalYear = await _unitOfWork.FiscalYears.GetByIdAsync(taxCalculationDto.FiscalYearId, cancellationToken);

            var compensationLabels = await LoadCompensationLabelMapAsync(cancellationToken);
            var calculationConfigByCode = await LoadSalaryLineCalculationConfigAsync(cancellationToken);
            var components = currentSalary.Components.ToList();
            var basicPeriodAmount = TaxCalculator.FindBasicPeriodAmount(components);

            var incomeLines = new List<TaxPlanningIncomeLineDto>();
            foreach (var component in currentSalary.Components)
            {
                var baseAmount = TaxCalculator.ResolvePercentageBaseAmount(component.ComponentCode, basicPeriodAmount, components, calculationConfigByCode);
                var periodAmount = TaxCalculator.ResolveAmount(component.ValueType, component.Value, baseAmount);
                var annualAmount = TaxCalculator.Annualize(periodAmount, component.FrequencyType);
                var incomeLine = new TaxPlanningIncomeLineDto
                {
                    Code = component.ComponentCode,
                    Label = ConfigLabelHelper.Resolve(compensationLabels, component.ComponentCode),
                    ValueType = component.ValueType,
                    AnnualAmount = annualAmount,
                    IsTaxable = component.IsTaxable
                };
                incomeLines.Add(incomeLine);
            }

            // Reuses TaxCalculation.InsuranceDeductionLines (already computed by
            // CalculateFromSalary against the same premiums/caps) instead of re-deriving the
            // eligible/capped amounts here a second time -- so this table can never disagree
            // with the InsuranceDeduction total above it.
            var insuranceLines = new List<TaxPlanningInsuranceLineDto>();
            foreach (var deductionLine in taxCalculationDto.TaxCalculation.InsuranceDeductionLines)
            {
                var insuranceLine = new TaxPlanningInsuranceLineDto
                {
                    InsuranceTypeCode = deductionLine.Code,
                    InsuranceTypeLabel = deductionLine.Label,
                    AnnualPremiumAmount = deductionLine.ActualAmount,
                    EligiblePercentage = deductionLine.EligiblePercentage,
                    CapAmount = deductionLine.CapAmount,
                    DeductedAmount = deductionLine.DeductedAmount,
                    AdditionalAmountAvailable = deductionLine.AdditionalAmountAvailable
                };
                insuranceLines.Add(insuranceLine);
            }

            var eligibleContributionAnnual = taxCalculationDto.TaxCalculation.RetirementContributionAnnual;
            var oneThirdOfTaxableIncome = Math.Round(taxCalculationDto.TaxCalculation.GrossAnnualIncome / 3m, 2);
            var maximumLimit = fiscalYear.RetirementExemptionCapAmount;
            var additionalContributionAvailable = Math.Max(0m, Math.Min(oneThirdOfTaxableIncome, maximumLimit) - eligibleContributionAnnual);

            var retirementFundDto = new RetirementFundBreakdownDto
            {
                EligibleContributionAnnual = eligibleContributionAnnual,
                OneThirdOfTaxableIncome = oneThirdOfTaxableIncome,
                MaximumLimit = maximumLimit,
                ExemptionApplied = taxCalculationDto.TaxCalculation.RetirementExemption,
                AdditionalContributionAvailable = additionalContributionAvailable
            };

            var taxPlanningDto = new TaxPlanningDto
            {
                EmployeeId = employeeId,
                SalaryId = taxCalculationDto.SalaryId,
                FiscalYearId = taxCalculationDto.FiscalYearId,
                FiscalYearCode = taxCalculationDto.FiscalYearCode,
                AssessmentType = currentSalary.AssessmentType.ToString(),
                IncomeLines = incomeLines,
                TotalAnnualIncome = taxCalculationDto.TaxCalculation.GrossAnnualIncome,
                RetirementFund = retirementFundDto,
                InsuranceLines = insuranceLines,
                InsuranceDeductionCapped = taxCalculationDto.TaxCalculation.InsuranceDeduction,
                TaxCalculation = taxCalculationDto.TaxCalculation,
                GrossMonthly = taxCalculationDto.GrossMonthly,
                NetMonthly = taxCalculationDto.NetMonthly
            };

            var taxPlanningSuccessResponse = CommonResponse<TaxPlanningDto>.Success(taxPlanningDto);
            return taxPlanningSuccessResponse;
        }

        // 2026-07-22: "salary receipt history" -- a 12-fiscal-month grid of every income line
        // plus the retirement-fund a/b/c/min breakdown, matching the reference HRMS's forecast
        // table. A month is Actual (real, disbursed figures from that month's own snapshotted
        // salary revision) once a real Approved/Paid PayrollRun slip exists for it; every other
        // month is Forecast (projected from the employee's *current* compensation plan, same
        // projection GetMonthlySalaryTaxCalculationAsync already uses). See
        // Docs/salary_annual_forecast_implementation_guide.md.
        public async Task<CommonResponse<SalaryAnnualForecastDto>> GetAnnualForecastAsync(Guid employeeId, Guid? fiscalYearId, CancellationToken cancellationToken = default)
        {
            var employee = await _unitOfWork.Employees.GetByIdAsync(employeeId, cancellationToken);
            if (employee == null)
            {
                var notFoundResponse = CommonResponse<SalaryAnnualForecastDto>.Fail(ResponseCodes.NotFound, "Employee with id '" + employeeId + "' was not found.");
                return notFoundResponse;
            }

            var fiscalYear = fiscalYearId.HasValue
                ? await _unitOfWork.FiscalYears.GetByIdAsync(fiscalYearId.Value, cancellationToken)
                : await _unitOfWork.FiscalYears.GetCurrentYearAsync(cancellationToken);
            if (fiscalYear == null)
            {
                var noFiscalYearResponse = CommonResponse<SalaryAnnualForecastDto>.Fail(ResponseCodes.NotFound, fiscalYearId.HasValue ? "Fiscal year with id '" + fiscalYearId.Value + "' was not found." : "No fiscal year is marked as current.");
                return noFiscalYearResponse;
            }

            var currentSalary = await _unitOfWork.Employees.GetCurrentSalaryAsync(employeeId, cancellationToken);
            if (currentSalary == null)
            {
                var noSalaryResponse = CommonResponse<SalaryAnnualForecastDto>.Fail(ResponseCodes.NotFound, "This employee has no salary on record yet.");
                return noSalaryResponse;
            }

            var insuranceTypeCaps = await BuildInsuranceTypeCapsAsync(cancellationToken);
            var compensationLabels = await LoadCompensationLabelMapAsync(cancellationToken);
            var calculationConfigByCode = await LoadSalaryLineCalculationConfigAsync(cancellationToken);

            // The Forecast baseline: the current compensation plan projected across all 12
            // months, same shape GetMonthlySalaryTaxCalculationAsync already builds. Every
            // month starts out Forecast; months with a real slip are overwritten below.
            var forecastTaxSlabs = await _unitOfWork.FiscalYears.GetTaxSlabsAsync(fiscalYear.Id, currentSalary.AssessmentType, cancellationToken);
            List<MonthlyBreakdownRowDto> forecastMonths = null;
            if (forecastTaxSlabs.Count > 0)
            {
                var forecastComponents = currentSalary.Components.ToList();
                var forecastDeductions = currentSalary.Deductions.ToList();
                var forecastTaxCalculation = TaxCalculator.CalculateFromSalary(
                    forecastComponents,
                    forecastDeductions,
                    currentSalary.InsurancePremiums.ToList(),
                    insuranceTypeCaps,
                    fiscalYear.RetirementExemptionCapAmount,
                    forecastTaxSlabs,
                    compensationLabels,
                    calculationConfigByCode);

                forecastMonths = MonthlyBreakdownCalculator.Build(
                    forecastComponents,
                    forecastDeductions,
                    currentSalary.EffectiveFromDate,
                    fiscalYear,
                    forecastTaxCalculation.AnnualTax,
                    compensationLabels,
                    calculationConfigByCode);
            }

            var actualSlips = await _unitOfWork.PayrollRuns.GetSlipsForYearAsync(employeeId, fiscalYear.Id, cancellationToken);
            var actualSlipsByMonth = new Dictionary<int, SalarySlip>();
            foreach (var actualSlip in actualSlips)
            {
                if (actualSlip.PayrollRun != null)
                {
                    actualSlipsByMonth[actualSlip.PayrollRun.MonthIndex] = actualSlip;
                }
            }

            var forecastDto = new SalaryAnnualForecastDto
            {
                EmployeeId = employeeId,
                FiscalYearId = fiscalYear.Id,
                FiscalYearCode = fiscalYear.Code
            };

            var incomeLinesByCode = new Dictionary<string, SalaryForecastLineDto>();
            var incomeLineOrder = new List<string>();
            var annualIncomeForecastLine = new SalaryForecastLineDto { Code = "ANNUAL_INCOME_FORECAST", Label = "Annual Income Forecast" };
            var ssfDeductionLine = new SalaryForecastLineDto { Code = "SSF_DEDUCTION_TOTAL", Label = "SSF Deduction" };
            var eligibleLine = new SalaryForecastLineDto { Code = "RETIREMENT_A", Label = "a) Sum of Eligible Retirement Fund and Social Security Fund" };
            var allowableLine = new SalaryForecastLineDto { Code = "RETIREMENT_B", Label = "b) Allowable Limit" };
            var oneThirdLine = new SalaryForecastLineDto { Code = "RETIREMENT_C", Label = "c) 1/3rd of Taxable Income" };
            var minLine = new SalaryForecastLineDto { Code = "RETIREMENT_MIN", Label = "Min of a, b or c" };

            for (var monthIndex = 1; monthIndex <= 12; monthIndex++)
            {
                var monthName = MonthlyBreakdownCalculator.GetMonthName(monthIndex);
                forecastDto.MonthNames.Add(monthName);

                actualSlipsByMonth.TryGetValue(monthIndex, out var actualSlip);
                var isActual = actualSlip != null;
                forecastDto.IsActualByMonth.Add(isActual);

                decimal monthGrossIncome;

                if (isActual)
                {
                    monthGrossIncome = 0m;
                    foreach (var slipLine in actualSlip.Lines)
                    {
                        if (slipLine.LineType != SalaryLineType.Earning)
                        {
                            continue;
                        }

                        AddOrUpdateMonthlyLine(incomeLinesByCode, incomeLineOrder, slipLine.ComponentCode ?? slipLine.Description, slipLine.Description, "Actual", monthIndex, slipLine.Amount);
                        monthGrossIncome += slipLine.Amount;
                    }

                    // The retirement-fund a/b/c/min breakdown is only ever shown for a month
                    // that's already real -- re-run the tax calculation against THAT month's own
                    // snapshotted salary revision (not the current plan, which may have changed
                    // since), so it matches exactly what was actually withheld that month.
                    var snapshotSalary = await _unitOfWork.Employees.GetSalaryWithLineItemsAsync(actualSlip.EmployeeSalaryId, cancellationToken);
                    if (snapshotSalary != null)
                    {
                        var snapshotTaxSlabs = await _unitOfWork.FiscalYears.GetTaxSlabsAsync(fiscalYear.Id, snapshotSalary.AssessmentType, cancellationToken);
                        if (snapshotTaxSlabs.Count > 0)
                        {
                            var snapshotTaxCalculation = TaxCalculator.CalculateFromSalary(
                                snapshotSalary.Components.ToList(),
                                snapshotSalary.Deductions.ToList(),
                                snapshotSalary.InsurancePremiums.ToList(),
                                insuranceTypeCaps,
                                fiscalYear.RetirementExemptionCapAmount,
                                snapshotTaxSlabs,
                                compensationLabels,
                                calculationConfigByCode);

                            var oneThird = Math.Round(snapshotTaxCalculation.GrossAnnualIncome / 3m, 2);
                            eligibleLine.MonthlyAmounts.Add(snapshotTaxCalculation.RetirementContributionAnnual);
                            allowableLine.MonthlyAmounts.Add(fiscalYear.RetirementExemptionCapAmount);
                            oneThirdLine.MonthlyAmounts.Add(oneThird);
                            minLine.MonthlyAmounts.Add(snapshotTaxCalculation.RetirementExemption);
                        }
                        else
                        {
                            eligibleLine.MonthlyAmounts.Add(null);
                            allowableLine.MonthlyAmounts.Add(null);
                            oneThirdLine.MonthlyAmounts.Add(null);
                            minLine.MonthlyAmounts.Add(null);
                        }
                    }
                    else
                    {
                        eligibleLine.MonthlyAmounts.Add(null);
                        allowableLine.MonthlyAmounts.Add(null);
                        oneThirdLine.MonthlyAmounts.Add(null);
                        minLine.MonthlyAmounts.Add(null);
                    }

                    decimal ssfDeductionThisMonth = 0m;
                    foreach (var slipLine in actualSlip.Lines)
                    {
                        if (slipLine.ComponentCode == SalaryDeductionCodes.SsfDeduction)
                        {
                            ssfDeductionThisMonth += slipLine.Amount;
                        }
                    }

                    ssfDeductionLine.MonthlyAmounts.Add(ssfDeductionThisMonth);
                }
                else
                {
                    monthGrossIncome = 0m;
                    if (forecastMonths != null)
                    {
                        var monthRow = forecastMonths[monthIndex - 1];
                        foreach (var incomeLine in monthRow.IncomeLines)
                        {
                            AddOrUpdateMonthlyLine(incomeLinesByCode, incomeLineOrder, incomeLine.Code, incomeLine.Label, "Forecast", monthIndex, incomeLine.Amount);
                        }

                        monthGrossIncome = monthRow.MonthGrossIncome;

                        decimal ssfDeductionForecast = 0m;
                        foreach (var deductionLine in monthRow.DeductionLines)
                        {
                            if (deductionLine.Code == SalaryDeductionCodes.SsfDeduction)
                            {
                                ssfDeductionForecast += deductionLine.Amount;
                            }
                        }

                        ssfDeductionLine.MonthlyAmounts.Add(ssfDeductionForecast);
                    }
                    else
                    {
                        ssfDeductionLine.MonthlyAmounts.Add(null);
                    }

                    // Not populated for a Forecast month -- see the DTO's own doc comment.
                    eligibleLine.MonthlyAmounts.Add(null);
                    allowableLine.MonthlyAmounts.Add(null);
                    oneThirdLine.MonthlyAmounts.Add(null);
                    minLine.MonthlyAmounts.Add(null);
                }

                annualIncomeForecastLine.MonthlyAmounts.Add(monthGrossIncome);
            }

            foreach (var code in incomeLineOrder)
            {
                var line = incomeLinesByCode[code];

                // A code that stopped appearing partway through the year (e.g. a one-time bonus
                // only paid in an earlier month) otherwise leaves its list short -- pad to
                // exactly 12 entries so every row lines up under the same 12 month columns.
                while (line.MonthlyAmounts.Count < 12)
                {
                    line.MonthlyAmounts.Add(null);
                }

                decimal annualTotal = 0m;
                foreach (var amount in line.MonthlyAmounts)
                {
                    annualTotal += amount ?? 0m;
                }

                line.AnnualAmount = annualTotal;
                forecastDto.IncomeLines.Add(line);
            }

            decimal annualIncomeTotal = 0m;
            foreach (var amount in annualIncomeForecastLine.MonthlyAmounts)
            {
                annualIncomeTotal += amount ?? 0m;
            }

            annualIncomeForecastLine.AnnualAmount = annualIncomeTotal;
            forecastDto.AnnualIncomeForecastLine = annualIncomeForecastLine;

            decimal ssfDeductionAnnual = 0m;
            foreach (var amount in ssfDeductionLine.MonthlyAmounts)
            {
                ssfDeductionAnnual += amount ?? 0m;
            }

            ssfDeductionLine.AnnualAmount = ssfDeductionAnnual;

            forecastDto.RetirementFundLines.Add(ssfDeductionLine);
            forecastDto.RetirementFundLines.Add(eligibleLine);
            forecastDto.RetirementFundLines.Add(allowableLine);
            forecastDto.RetirementFundLines.Add(oneThirdLine);
            forecastDto.RetirementFundLines.Add(minLine);

            var successResponse = CommonResponse<SalaryAnnualForecastDto>.Success(forecastDto);
            return successResponse;
        }

        // 2026-07-23: flat spreadsheet-row shape for the "Tax Details" tab, matching a reference
        // HRMS's table exactly (one row per Particulars line, one column per fiscal month,
        // header/actual-vs-forecast flags instead of a nested months[] structure). Reuses the same
        // Actual (real SalarySlip) vs Forecast (projected MonthlyBreakdownCalculator) split
        // GetAnnualForecastAsync already established, but adds one more concept: a single
        // "assessment month" (whichever fiscal month contains today, per Nepal time) whose own
        // full tax computation drives the retirement-fund a/b/c/min, per-slab SST/TDS, and
        // paid/remaining/this-month rows -- these are a "current status" snapshot, not something
        // meaningful to project across all 12 months, so they're populated only in that one
        // column (0 everywhere else), same convention the a/b/c/min rows already used.
        public async Task<CommonResponse<TaxDetailsGridDto>> GetTaxDetailsGridAsync(Guid employeeId, Guid? fiscalYearId, CancellationToken cancellationToken = default)
        {
            var employee = await _unitOfWork.Employees.GetByIdAsync(employeeId, cancellationToken);
            if (employee == null)
            {
                var notFoundResponse = CommonResponse<TaxDetailsGridDto>.Fail(ResponseCodes.NotFound, "Employee with id '" + employeeId + "' was not found.");
                return notFoundResponse;
            }

            var fiscalYear = fiscalYearId.HasValue
                ? await _unitOfWork.FiscalYears.GetByIdAsync(fiscalYearId.Value, cancellationToken)
                : await _unitOfWork.FiscalYears.GetCurrentYearAsync(cancellationToken);
            if (fiscalYear == null)
            {
                var noFiscalYearResponse = CommonResponse<TaxDetailsGridDto>.Fail(ResponseCodes.NotFound, fiscalYearId.HasValue ? "Fiscal year with id '" + fiscalYearId.Value + "' was not found." : "No fiscal year is marked as current.");
                return noFiscalYearResponse;
            }

            var currentSalary = await _unitOfWork.Employees.GetCurrentSalaryAsync(employeeId, cancellationToken);
            if (currentSalary == null)
            {
                var noSalaryResponse = CommonResponse<TaxDetailsGridDto>.Fail(ResponseCodes.NotFound, "This employee has no salary on record yet.");
                return noSalaryResponse;
            }

            var insuranceTypeCaps = await BuildInsuranceTypeCapsAsync(cancellationToken);
            var compensationLabels = await LoadCompensationLabelMapAsync(cancellationToken);
            var calculationConfigByCode = await LoadSalaryLineCalculationConfigAsync(cancellationToken);

            var forecastTaxSlabs = await _unitOfWork.FiscalYears.GetTaxSlabsAsync(fiscalYear.Id, currentSalary.AssessmentType, cancellationToken);
            List<MonthlyBreakdownRowDto> forecastMonths = null;
            if (forecastTaxSlabs.Count > 0)
            {
                var forecastComponents = currentSalary.Components.ToList();
                var forecastDeductions = currentSalary.Deductions.ToList();
                var forecastTaxCalculationForMonths = TaxCalculator.CalculateFromSalary(
                    forecastComponents,
                    forecastDeductions,
                    currentSalary.InsurancePremiums.ToList(),
                    insuranceTypeCaps,
                    fiscalYear.RetirementExemptionCapAmount,
                    forecastTaxSlabs,
                    compensationLabels,
                    calculationConfigByCode);

                forecastMonths = MonthlyBreakdownCalculator.Build(
                    forecastComponents,
                    forecastDeductions,
                    currentSalary.EffectiveFromDate,
                    fiscalYear,
                    forecastTaxCalculationForMonths.AnnualTax,
                    compensationLabels,
                    calculationConfigByCode);
            }

            var actualSlips = await _unitOfWork.PayrollRuns.GetSlipsForYearAsync(employeeId, fiscalYear.Id, cancellationToken);
            var actualSlipsByMonth = new Dictionary<int, SalarySlip>();
            foreach (var actualSlip in actualSlips)
            {
                if (actualSlip.PayrollRun != null)
                {
                    actualSlipsByMonth[actualSlip.PayrollRun.MonthIndex] = actualSlip;
                }
            }

            var assessmentMonthIndex = ResolveAssessmentMonthIndex(fiscalYear, NepalDateHelper.GetNepalToday());

            var incomeLinesByCode = new Dictionary<string, SalaryForecastLineDto>();
            var incomeLineOrder = new List<string>();
            var annualIncomeLine = new SalaryForecastLineDto { Code = "ANNUAL_INCOME_FORECAST", Label = "Annual Income Forecast" };
            var ssfDeductionLine = new SalaryForecastLineDto { Code = "SSF_DEDUCTION_TOTAL", Label = "SSF Deduction" };

            TaxCalculationResultDto assessmentTaxCalculation = null;
            decimal sstPaidInPastMonths = 0m;
            decimal tdsPaidInPastMonths = 0m;

            for (var monthIndex = 1; monthIndex <= 12; monthIndex++)
            {
                actualSlipsByMonth.TryGetValue(monthIndex, out var actualSlip);
                var isActual = actualSlip != null;

                decimal monthGrossIncome = 0m;

                if (isActual)
                {
                    foreach (var slipLine in actualSlip.Lines)
                    {
                        if (slipLine.LineType != SalaryLineType.Earning)
                        {
                            continue;
                        }

                        AddOrUpdateMonthlyLine(incomeLinesByCode, incomeLineOrder, slipLine.ComponentCode ?? slipLine.Description, slipLine.Description, null, monthIndex, slipLine.Amount);
                        monthGrossIncome += slipLine.Amount;
                    }

                    decimal ssfDeductionThisMonth = 0m;
                    foreach (var slipLine in actualSlip.Lines)
                    {
                        if (slipLine.ComponentCode == SalaryDeductionCodes.SsfDeduction)
                        {
                            ssfDeductionThisMonth += slipLine.Amount;
                        }
                    }

                    ssfDeductionLine.MonthlyAmounts.Add(ssfDeductionThisMonth);

                    if (monthIndex < assessmentMonthIndex)
                    {
                        var pastCalculation = await ComputeSnapshotTaxCalculationAsync(actualSlip, fiscalYear, insuranceTypeCaps, compensationLabels, calculationConfigByCode, cancellationToken);
                        if (pastCalculation != null)
                        {
                            var pastSplit = SplitSstAndTds(pastCalculation.Breakdown);
                            sstPaidInPastMonths += pastSplit.Sst;
                            tdsPaidInPastMonths += pastSplit.Tds;
                        }
                    }
                    else if (monthIndex == assessmentMonthIndex)
                    {
                        assessmentTaxCalculation = await ComputeSnapshotTaxCalculationAsync(actualSlip, fiscalYear, insuranceTypeCaps, compensationLabels, calculationConfigByCode, cancellationToken);
                    }
                }
                else
                {
                    if (forecastMonths != null)
                    {
                        var monthRow = forecastMonths[monthIndex - 1];
                        foreach (var incomeLine in monthRow.IncomeLines)
                        {
                            AddOrUpdateMonthlyLine(incomeLinesByCode, incomeLineOrder, incomeLine.Code, incomeLine.Label, null, monthIndex, incomeLine.Amount);
                        }

                        monthGrossIncome = monthRow.MonthGrossIncome;

                        decimal ssfDeductionForecast = 0m;
                        foreach (var deductionLine in monthRow.DeductionLines)
                        {
                            if (deductionLine.Code == SalaryDeductionCodes.SsfDeduction)
                            {
                                ssfDeductionForecast += deductionLine.Amount;
                            }
                        }

                        ssfDeductionLine.MonthlyAmounts.Add(ssfDeductionForecast);
                    }
                    else
                    {
                        ssfDeductionLine.MonthlyAmounts.Add(null);
                    }

                    if (monthIndex == assessmentMonthIndex && forecastTaxSlabs.Count > 0)
                    {
                        assessmentTaxCalculation = TaxCalculator.CalculateFromSalary(
                            currentSalary.Components.ToList(),
                            currentSalary.Deductions.ToList(),
                            currentSalary.InsurancePremiums.ToList(),
                            insuranceTypeCaps,
                            fiscalYear.RetirementExemptionCapAmount,
                            forecastTaxSlabs,
                            compensationLabels,
                            calculationConfigByCode);
                    }
                }

                annualIncomeLine.MonthlyAmounts.Add(monthGrossIncome);
            }

            foreach (var code in incomeLineOrder)
            {
                var line = incomeLinesByCode[code];
                while (line.MonthlyAmounts.Count < 12)
                {
                    line.MonthlyAmounts.Add(null);
                }

                decimal annualTotal = 0m;
                foreach (var amount in line.MonthlyAmounts)
                {
                    annualTotal += amount ?? 0m;
                }

                line.AnnualAmount = annualTotal;
            }

            decimal annualIncomeTotal = 0m;
            foreach (var amount in annualIncomeLine.MonthlyAmounts)
            {
                annualIncomeTotal += amount ?? 0m;
            }

            annualIncomeLine.AnnualAmount = annualIncomeTotal;

            decimal ssfDeductionAnnual = 0m;
            foreach (var amount in ssfDeductionLine.MonthlyAmounts)
            {
                ssfDeductionAnnual += amount ?? 0m;
            }

            ssfDeductionLine.AnnualAmount = ssfDeductionAnnual;

            var gridDto = new TaxDetailsGridDto
            {
                Name = BuildFullName(employee.FirstName, employee.MiddleName, employee.LastName),
                Gender = employee.Gender.ToString(),
                TaxPaidAs = currentSalary.AssessmentType.ToString(),
                IsHandicapped = false
            };

            for (var monthIndex = 1; monthIndex <= 12; monthIndex++)
            {
                gridDto.SetMonthIsForecast(monthIndex, !actualSlipsByMonth.ContainsKey(monthIndex));
            }

            var rows = gridDto.List;
            var rowNumber = 0;

            rowNumber++;
            rows.Add(BuildGridHeaderRow(rowNumber, "Incomes"));

            foreach (var code in incomeLineOrder)
            {
                var line = incomeLinesByCode[code];
                rowNumber++;
                rows.Add(BuildGridLineRow(rowNumber, line, ResolveIncomeLineDescription(code, currentSalary), isHeader: false));
            }

            rowNumber++;
            rows.Add(BuildGridLineRow(rowNumber, annualIncomeLine, null, isHeader: true));

            rowNumber++;
            rows.Add(BuildGridHeaderRow(rowNumber, "Retirement Fund"));

            rowNumber++;
            rows.Add(BuildGridLineRow(rowNumber, ssfDeductionLine, null, isHeader: false));

            rowNumber++;
            var eligibleRow = NewGridRow(rowNumber, "a) Sum of Eligible Retirement Fund and Social Security Fund", null, false);
            rowNumber++;
            var allowableRow = NewGridRow(rowNumber, "b) Allowable Limit", null, false);
            rowNumber++;
            var oneThirdRow = NewGridRow(rowNumber, "c) 1/3 of Taxable Income", null, false);
            rowNumber++;
            var minRow = NewGridRow(rowNumber, "Min of a, b or c", null, false);
            rowNumber++;
            var taxableRow = NewGridRow(rowNumber, "Annual Adjusted Taxable Amount", "Calculated on", true);

            if (assessmentTaxCalculation != null)
            {
                eligibleRow.SetMonthAmount(assessmentMonthIndex, assessmentTaxCalculation.RetirementContributionAnnual);
                allowableRow.SetMonthAmount(assessmentMonthIndex, fiscalYear.RetirementExemptionCapAmount);
                var oneThirdAmount = Math.Round(assessmentTaxCalculation.GrossAnnualIncome / 3m, 2);
                oneThirdRow.SetMonthAmount(assessmentMonthIndex, oneThirdAmount);
                minRow.SetMonthAmount(assessmentMonthIndex, assessmentTaxCalculation.RetirementExemption);
                taxableRow.SetMonthAmount(assessmentMonthIndex, assessmentTaxCalculation.AnnualTaxableIncome);
            }

            eligibleRow.Total = eligibleRow.GetMonthAmount(assessmentMonthIndex);
            allowableRow.Total = allowableRow.GetMonthAmount(assessmentMonthIndex);
            oneThirdRow.Total = oneThirdRow.GetMonthAmount(assessmentMonthIndex);
            minRow.Total = minRow.GetMonthAmount(assessmentMonthIndex);
            taxableRow.Total = taxableRow.GetMonthAmount(assessmentMonthIndex);

            rows.Add(eligibleRow);
            rows.Add(allowableRow);
            rows.Add(oneThirdRow);
            rows.Add(minRow);
            rows.Add(taxableRow);

            decimal totalSstForYear = 0m;
            decimal totalTdsForYear = 0m;

            if (assessmentTaxCalculation != null)
            {
                for (var slabIndex = 0; slabIndex < assessmentTaxCalculation.Breakdown.Count; slabIndex++)
                {
                    var slab = assessmentTaxCalculation.Breakdown[slabIndex];
                    var isSst = slabIndex == 0;
                    var displayRatePercent = isSst && slab.IsSsfExempted ? 0m : slab.TaxRate * 100m;
                    var label = (isSst ? "SST " : "TDS ") + displayRatePercent.ToString("0.##") + "%";

                    rowNumber++;
                    var slabRow = NewGridRow(rowNumber, label, slab.TaxableAmountInSlab.ToString("N2"), false);
                    slabRow.SetMonthAmount(assessmentMonthIndex, slab.TaxForSlab);
                    slabRow.Total = slab.TaxForSlab;
                    rows.Add(slabRow);

                    if (isSst)
                    {
                        totalSstForYear += slab.TaxForSlab;
                    }
                    else
                    {
                        totalTdsForYear += slab.TaxForSlab;
                    }
                }
            }

            rowNumber++;
            var totalSstRow = NewGridRow(rowNumber, "Total SST for the Year", null, true);
            totalSstRow.SetMonthAmount(assessmentMonthIndex, totalSstForYear);
            totalSstRow.Total = totalSstForYear;
            rows.Add(totalSstRow);

            rowNumber++;
            var totalTdsRow = NewGridRow(rowNumber, "Total TDS for the Year", null, true);
            totalTdsRow.SetMonthAmount(assessmentMonthIndex, totalTdsForYear);
            totalTdsRow.Total = totalTdsForYear;
            rows.Add(totalTdsRow);

            rowNumber++;
            var sstPaidRow = NewGridRow(rowNumber, "SST Paid in Past Month", null, false);
            sstPaidRow.SetMonthAmount(assessmentMonthIndex, sstPaidInPastMonths);
            sstPaidRow.Total = sstPaidInPastMonths;
            rows.Add(sstPaidRow);

            rowNumber++;
            var tdsPaidRow = NewGridRow(rowNumber, "TDS Paid in Past Month", null, false);
            tdsPaidRow.SetMonthAmount(assessmentMonthIndex, tdsPaidInPastMonths);
            tdsPaidRow.Total = tdsPaidInPastMonths;
            rows.Add(tdsPaidRow);

            var remainingSst = Math.Max(0m, totalSstForYear - sstPaidInPastMonths);
            var remainingTds = Math.Max(0m, totalTdsForYear - tdsPaidInPastMonths);

            rowNumber++;
            var remainingSstRow = NewGridRow(rowNumber, "Remaining SST in 12 month", null, false);
            remainingSstRow.SetMonthAmount(assessmentMonthIndex, remainingSst);
            remainingSstRow.Total = remainingSst;
            rows.Add(remainingSstRow);

            rowNumber++;
            var remainingTdsRow = NewGridRow(rowNumber, "Remaining TDS in 12 month", null, false);
            remainingTdsRow.SetMonthAmount(assessmentMonthIndex, remainingTds);
            remainingTdsRow.Total = remainingTds;
            rows.Add(remainingTdsRow);

            // Months from the assessment month through fiscal-month 12, inclusive -- the
            // remaining-liability amortization window ("this month" plus every month still ahead).
            var monthsRemainingIncludingThisOne = 13 - assessmentMonthIndex;
            var sstThisMonth = Math.Round(remainingSst / monthsRemainingIncludingThisOne, 2);
            var tdsThisMonth = Math.Round(remainingTds / monthsRemainingIncludingThisOne, 2);

            rowNumber++;
            var sstThisMonthRow = NewGridRow(rowNumber, "SST this Month", null, false);
            sstThisMonthRow.SetMonthAmount(assessmentMonthIndex, sstThisMonth);
            sstThisMonthRow.Total = sstThisMonth;
            rows.Add(sstThisMonthRow);

            rowNumber++;
            var tdsThisMonthRow = NewGridRow(rowNumber, "TDS this Month", null, false);
            tdsThisMonthRow.SetMonthAmount(assessmentMonthIndex, tdsThisMonth);
            tdsThisMonthRow.Total = tdsThisMonth;
            rows.Add(tdsThisMonthRow);

            var successResponse = CommonResponse<TaxDetailsGridDto>.Success(gridDto);
            return successResponse;
        }

        // Same day-range split MonthlyBreakdownCalculator.Build uses internally, exposed here so
        // GetTaxDetailsGridAsync can find which fiscal month contains "today" (Nepal time). A
        // fiscal year that doesn't contain today at all (fully past or fully future) falls back to
        // month 1 (nothing has happened yet) or month 12 (assess as of the final month) --
        // whichever edge is closer.
        private static int ResolveAssessmentMonthIndex(FiscalYear fiscalYear, DateTime today)
        {
            var totalDays = (fiscalYear.EndDate.Date - fiscalYear.StartDate.Date).Days + 1;
            var periodStart = fiscalYear.StartDate.Date;

            for (var monthIndex = 1; monthIndex <= 12; monthIndex++)
            {
                var periodEnd = monthIndex == 12
                    ? fiscalYear.EndDate.Date
                    : fiscalYear.StartDate.Date.AddDays((double)(Math.Round(totalDays * monthIndex / 12m) - 1));

                if (today >= periodStart && today <= periodEnd)
                {
                    return monthIndex;
                }

                periodStart = periodEnd.AddDays(1);
            }

            return today < fiscalYear.StartDate.Date ? 1 : 12;
        }

        // Re-runs TaxCalculator against one actual month's own snapshotted salary revision (not
        // the current plan, which may have changed since) -- used both for the assessment month's
        // own breakdown and for summing what earlier actual months already withheld. Null when the
        // snapshot salary or that assessment type's tax slabs no longer resolve.
        private async Task<TaxCalculationResultDto> ComputeSnapshotTaxCalculationAsync(SalarySlip actualSlip, FiscalYear fiscalYear, IReadOnlyDictionary<string, InsuranceCapConfig> insuranceTypeCaps, IReadOnlyDictionary<string, string> compensationLabels, IReadOnlyDictionary<string, SalaryLineCalculationConfig> calculationConfigByCode, CancellationToken cancellationToken)
        {
            var snapshotSalary = await _unitOfWork.Employees.GetSalaryWithLineItemsAsync(actualSlip.EmployeeSalaryId, cancellationToken);
            if (snapshotSalary == null)
            {
                return null;
            }

            var snapshotTaxSlabs = await _unitOfWork.FiscalYears.GetTaxSlabsAsync(fiscalYear.Id, snapshotSalary.AssessmentType, cancellationToken);
            if (snapshotTaxSlabs.Count == 0)
            {
                return null;
            }

            var snapshotTaxCalculation = TaxCalculator.CalculateFromSalary(
                snapshotSalary.Components.ToList(),
                snapshotSalary.Deductions.ToList(),
                snapshotSalary.InsurancePremiums.ToList(),
                insuranceTypeCaps,
                fiscalYear.RetirementExemptionCapAmount,
                snapshotTaxSlabs,
                compensationLabels,
                calculationConfigByCode);

            return snapshotTaxCalculation;
        }

        // The first slab (index 0) is always the "Social Security Tax" bracket by construction
        // (TaxCalculator.Calculate); every other slab is "TDS" -- see the SSF Social Security Tax
        // waiver note on TaxCalculator.Calculate.
        private static (decimal Sst, decimal Tds) SplitSstAndTds(List<TaxSlabBreakdownDto> breakdown)
        {
            decimal sst = 0m;
            decimal tds = 0m;

            for (var slabIndex = 0; slabIndex < breakdown.Count; slabIndex++)
            {
                if (slabIndex == 0)
                {
                    sst += breakdown[slabIndex].TaxForSlab;
                }
                else
                {
                    tds += breakdown[slabIndex].TaxForSlab;
                }
            }

            return (sst, tds);
        }

        // A component's own FrequencyType on the CURRENT compensation plan, translated to the
        // reference UI's description text. Falls back to null (blank) for a code that no longer
        // appears on the current plan (e.g. it only ever existed on a past, since-superseded
        // revision) -- there's no "as of that month" frequency to describe in that case.
        private static string ResolveIncomeLineDescription(string code, EmployeeSalary currentSalary)
        {
            foreach (var component in currentSalary.Components)
            {
                if (component.ComponentCode != code)
                {
                    continue;
                }

                if (component.FrequencyType == PayFrequencyType.Monthly)
                {
                    return "Fixed Income";
                }

                if (component.FrequencyType == PayFrequencyType.Annual)
                {
                    return "Annual Income";
                }

                return "One Time Income";
            }

            return null;
        }

        private static TaxDetailsGridRowDto NewGridRow(int rowNumber, string particulars, string description, bool isHeader)
        {
            var row = new TaxDetailsGridRowDto
            {
                RowNumber = rowNumber,
                IsHeader = isHeader,
                Particulars = particulars,
                Description = description
            };

            return row;
        }

        private static TaxDetailsGridRowDto BuildGridHeaderRow(int rowNumber, string particulars)
        {
            return NewGridRow(rowNumber, particulars, null, true);
        }

        private static TaxDetailsGridRowDto BuildGridLineRow(int rowNumber, SalaryForecastLineDto line, string description, bool isHeader)
        {
            var row = NewGridRow(rowNumber, line.Label, description, isHeader);
            row.Total = line.AnnualAmount;

            for (var monthIndex = 1; monthIndex <= 12; monthIndex++)
            {
                row.SetMonthAmount(monthIndex, line.MonthlyAmounts[monthIndex - 1] ?? 0m);
            }

            return row;
        }

        // Shared by GetAnnualForecastAsync's Actual (real slip lines) and Forecast (projected
        // MonthlyBreakdownCalculator rows) paths -- both resolve to the same
        // Dictionary<code, SalaryForecastLineDto>/order-list pair so a code that appears in both
        // an Actual and a Forecast month lands on the same row.
        private static void AddOrUpdateMonthlyLine(Dictionary<string, SalaryForecastLineDto> linesByCode, List<string> order, string code, string label, string description, int monthIndex, decimal amount)
        {
            if (!linesByCode.TryGetValue(code, out var line))
            {
                line = new SalaryForecastLineDto { Code = code, Label = label, Description = description };
                for (var i = 1; i < monthIndex; i++)
                {
                    line.MonthlyAmounts.Add(null);
                }

                linesByCode[code] = line;
                order.Add(code);
            }

            while (line.MonthlyAmounts.Count < monthIndex - 1)
            {
                line.MonthlyAmounts.Add(null);
            }

            line.MonthlyAmounts.Add(amount);
        }

        public async Task<CommonResponse<SalaryComponentDto>> AddSalaryComponentAsync(Guid employeeId, Guid salaryId, SalaryComponentInput command, CancellationToken cancellationToken = default)
        {
            var validationResult = _componentValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<SalaryComponentDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var salary = await _unitOfWork.Employees.GetSalaryByIdAsync(salaryId, cancellationToken);
            if (salary == null || salary.EmployeeId != employeeId)
            {
                var notFoundResponse = CommonResponse<SalaryComponentDto>.Fail(ResponseCodes.NotFound, "Salary revision was not found on this employee.");
                return notFoundResponse;
            }

            var componentCode = command.ComponentCode.Trim();
            var componentCatalogOption = await _unitOfWork.Configs.GetByTypeCodeAndCodeAsync(ConfigTypeCodes.SalaryComponentType, componentCode, cancellationToken);
            if (componentCatalogOption == null)
            {
                var invalidCodeResponse = CommonResponse<SalaryComponentDto>.Fail(ResponseCodes.ValidationError, "ComponentCode '" + componentCode + "' is not a known salary component option.");
                return invalidCodeResponse;
            }

            var percentageLockError = SalaryLineCalculationHelper.ValidatePercentageLock(componentCatalogOption, command.ValueType, command.Value);
            if (percentageLockError != null)
            {
                var percentageLockResponse = CommonResponse<SalaryComponentDto>.Fail(ResponseCodes.ValidationError, percentageLockError);
                return percentageLockResponse;
            }

            var calculateTypeError = SalaryLineCalculationHelper.ValidateCalculateType(componentCatalogOption, SalaryLineCalculateTypes.Addition);
            if (calculateTypeError != null)
            {
                var calculateTypeResponse = CommonResponse<SalaryComponentDto>.Fail(ResponseCodes.ValidationError, calculateTypeError);
                return calculateTypeResponse;
            }

            var frequencyLockError = SalaryLineCalculationHelper.ValidateFrequencyLock(componentCatalogOption, command.FrequencyType);
            if (frequencyLockError != null)
            {
                var frequencyLockResponse = CommonResponse<SalaryComponentDto>.Fail(ResponseCodes.ValidationError, frequencyLockError);
                return frequencyLockResponse;
            }

            var component = new EmployeeSalaryComponent
            {
                EmployeeSalaryId = salaryId,
                ComponentCode = componentCode,
                ValueType = command.ValueType,
                Value = command.Value,
                FrequencyType = command.FrequencyType,
                IsTaxable = command.IsTaxable,
                IsRetirementContribution = command.IsRetirementContribution,
                EmployeeSalary = salary
            };

            await _unitOfWork.Employees.AddSalaryComponentAsync(component, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var componentDto = EmployeeMapper.ToComponentDto(component, await LoadCompensationLabelMapAsync(cancellationToken));
            var successResponse = CommonResponse<SalaryComponentDto>.Success(componentDto, "Salary component added successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<bool>> RemoveSalaryComponentAsync(Guid employeeId, Guid salaryId, Guid componentId, CancellationToken cancellationToken = default)
        {
            var component = await _unitOfWork.Employees.GetSalaryComponentByIdAsync(componentId, cancellationToken);
            if (component == null || component.EmployeeSalaryId != salaryId)
            {
                var notFoundResponse = CommonResponse<bool>.Fail(ResponseCodes.NotFound, "Salary component was not found on this salary revision.");
                return notFoundResponse;
            }

            _unitOfWork.Employees.RemoveSalaryComponent(component);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var successResponse = CommonResponse<bool>.Success(true, "Salary component removed successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<SalaryDeductionDto>> AddSalaryDeductionAsync(Guid employeeId, Guid salaryId, SalaryDeductionInput command, CancellationToken cancellationToken = default)
        {
            var validationResult = _deductionValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<SalaryDeductionDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var salary = await _unitOfWork.Employees.GetSalaryByIdAsync(salaryId, cancellationToken);
            if (salary == null || salary.EmployeeId != employeeId)
            {
                var notFoundResponse = CommonResponse<SalaryDeductionDto>.Fail(ResponseCodes.NotFound, "Salary revision was not found on this employee.");
                return notFoundResponse;
            }

            var deductionCode = command.DeductionCode.Trim();
            var deductionCatalogOption = await _unitOfWork.Configs.GetByTypeCodeAndCodeAsync(ConfigTypeCodes.DeductionType, deductionCode, cancellationToken);
            if (deductionCatalogOption == null)
            {
                var invalidCodeResponse = CommonResponse<SalaryDeductionDto>.Fail(ResponseCodes.ValidationError, "DeductionCode '" + deductionCode + "' is not a known deduction option.");
                return invalidCodeResponse;
            }

            var percentageLockError = SalaryLineCalculationHelper.ValidatePercentageLock(deductionCatalogOption, command.ValueType, command.Value);
            if (percentageLockError != null)
            {
                var percentageLockResponse = CommonResponse<SalaryDeductionDto>.Fail(ResponseCodes.ValidationError, percentageLockError);
                return percentageLockResponse;
            }

            var calculateTypeError = SalaryLineCalculationHelper.ValidateCalculateType(deductionCatalogOption, SalaryLineCalculateTypes.Deduction);
            if (calculateTypeError != null)
            {
                var calculateTypeResponse = CommonResponse<SalaryDeductionDto>.Fail(ResponseCodes.ValidationError, calculateTypeError);
                return calculateTypeResponse;
            }

            var frequencyLockError = SalaryLineCalculationHelper.ValidateFrequencyLock(deductionCatalogOption, command.FrequencyType);
            if (frequencyLockError != null)
            {
                var frequencyLockResponse = CommonResponse<SalaryDeductionDto>.Fail(ResponseCodes.ValidationError, frequencyLockError);
                return frequencyLockResponse;
            }

            var deduction = new EmployeeSalaryDeduction
            {
                EmployeeSalaryId = salaryId,
                DeductionCode = deductionCode,
                ValueType = command.ValueType,
                Value = command.Value,
                FrequencyType = command.FrequencyType,
                IsRetirementContribution = command.IsRetirementContribution,
                EmployeeSalary = salary
            };

            await _unitOfWork.Employees.AddSalaryDeductionAsync(deduction, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var deductionDto = EmployeeMapper.ToDeductionDto(deduction, await LoadCompensationLabelMapAsync(cancellationToken));
            var successResponse = CommonResponse<SalaryDeductionDto>.Success(deductionDto, "Salary deduction added successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<bool>> RemoveSalaryDeductionAsync(Guid employeeId, Guid salaryId, Guid deductionId, CancellationToken cancellationToken = default)
        {
            var deduction = await _unitOfWork.Employees.GetSalaryDeductionByIdAsync(deductionId, cancellationToken);
            if (deduction == null || deduction.EmployeeSalaryId != salaryId)
            {
                var notFoundResponse = CommonResponse<bool>.Fail(ResponseCodes.NotFound, "Salary deduction was not found on this salary revision.");
                return notFoundResponse;
            }

            _unitOfWork.Employees.RemoveSalaryDeduction(deduction);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var successResponse = CommonResponse<bool>.Success(true, "Salary deduction removed successfully.");
            return successResponse;
        }

        // 2026-07-23: resolves Code against SalaryComponentType first, then DeductionType, and
        // delegates to the matching single-table method above -- same validation (catalog
        // existence, percentage lock, CalculateType, frequency lock) either way, just reached
        // through one code-driven entry point instead of the caller having to know up front
        // which table a code belongs to.
        public async Task<CommonResponse<SalaryLineDto>> AddSalaryLineAsync(Guid employeeId, Guid salaryId, SalaryLineInput command, CancellationToken cancellationToken = default)
        {
            var validationResult = _lineValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<SalaryLineDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var code = command.Code.Trim();

            var componentCatalogOption = await _unitOfWork.Configs.GetByTypeCodeAndCodeAsync(ConfigTypeCodes.SalaryComponentType, code, cancellationToken);
            if (componentCatalogOption != null)
            {
                var componentInput = new SalaryComponentInput
                {
                    ComponentCode = code,
                    ValueType = command.ValueType,
                    Value = command.Value,
                    FrequencyType = command.FrequencyType,
                    IsTaxable = command.IsTaxable,
                    IsRetirementContribution = command.IsRetirementContribution
                };

                var componentResponse = await AddSalaryComponentAsync(employeeId, salaryId, componentInput, cancellationToken);
                var componentLineResponse = componentResponse.ResponseCode == ResponseCodes.Success
                    ? CommonResponse<SalaryLineDto>.Success(EmployeeMapper.ToLineDto(componentResponse.Data), componentResponse.ResponseMessage)
                    : CommonResponse<SalaryLineDto>.Fail(componentResponse.ResponseCode, componentResponse.ResponseMessage);
                return componentLineResponse;
            }

            var deductionCatalogOption = await _unitOfWork.Configs.GetByTypeCodeAndCodeAsync(ConfigTypeCodes.DeductionType, code, cancellationToken);
            if (deductionCatalogOption != null)
            {
                var deductionInput = new SalaryDeductionInput
                {
                    DeductionCode = code,
                    ValueType = command.ValueType,
                    Value = command.Value,
                    FrequencyType = command.FrequencyType,
                    IsRetirementContribution = command.IsRetirementContribution
                };

                var deductionResponse = await AddSalaryDeductionAsync(employeeId, salaryId, deductionInput, cancellationToken);
                var deductionLineResponse = deductionResponse.ResponseCode == ResponseCodes.Success
                    ? CommonResponse<SalaryLineDto>.Success(EmployeeMapper.ToLineDto(deductionResponse.Data), deductionResponse.ResponseMessage)
                    : CommonResponse<SalaryLineDto>.Fail(deductionResponse.ResponseCode, deductionResponse.ResponseMessage);
                return deductionLineResponse;
            }

            var notFoundResponse = CommonResponse<SalaryLineDto>.Fail(ResponseCodes.ValidationError, "Code '" + code + "' is not a known salary component or deduction option.");
            return notFoundResponse;
        }

        // Tries the component table first, then the deduction table -- a caller working off the
        // unified SalaryLineDto shape has no other way to know which table a given line id lives
        // in.
        public async Task<CommonResponse<bool>> RemoveSalaryLineAsync(Guid employeeId, Guid salaryId, Guid lineId, CancellationToken cancellationToken = default)
        {
            var component = await _unitOfWork.Employees.GetSalaryComponentByIdAsync(lineId, cancellationToken);
            if (component != null && component.EmployeeSalaryId == salaryId)
            {
                return await RemoveSalaryComponentAsync(employeeId, salaryId, lineId, cancellationToken);
            }

            var deduction = await _unitOfWork.Employees.GetSalaryDeductionByIdAsync(lineId, cancellationToken);
            if (deduction != null && deduction.EmployeeSalaryId == salaryId)
            {
                return await RemoveSalaryDeductionAsync(employeeId, salaryId, lineId, cancellationToken);
            }

            var notFoundResponse = CommonResponse<bool>.Fail(ResponseCodes.NotFound, "Salary line was not found on this salary revision.");
            return notFoundResponse;
        }

        public async Task<CommonResponse<InsurancePremiumDto>> AddInsurancePremiumAsync(Guid employeeId, Guid salaryId, InsurancePremiumInput command, CancellationToken cancellationToken = default)
        {
            var validationResult = _premiumValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<InsurancePremiumDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var salary = await _unitOfWork.Employees.GetSalaryByIdAsync(salaryId, cancellationToken);
            if (salary == null || salary.EmployeeId != employeeId)
            {
                var notFoundResponse = CommonResponse<InsurancePremiumDto>.Fail(ResponseCodes.NotFound, "Salary revision was not found on this employee.");
                return notFoundResponse;
            }

            var insuranceTypeCode = command.InsuranceTypeCode.Trim();
            var codeExists = await _unitOfWork.Configs.CodeExistsAsync(ConfigTypeCodes.InsuranceType, insuranceTypeCode, cancellationToken);
            if (!codeExists)
            {
                var invalidCodeResponse = CommonResponse<InsurancePremiumDto>.Fail(ResponseCodes.ValidationError, "InsuranceTypeCode '" + insuranceTypeCode + "' is not a known insurance type option.");
                return invalidCodeResponse;
            }

            var premium = new EmployeeInsurancePremium
            {
                EmployeeSalaryId = salaryId,
                InsuranceTypeCode = insuranceTypeCode,
                AnnualPremiumAmount = command.AnnualPremiumAmount,
                EmployeeSalary = salary
            };

            await _unitOfWork.Employees.AddInsurancePremiumAsync(premium, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var premiumDto = EmployeeMapper.ToInsurancePremiumDto(premium, await LoadCompensationLabelMapAsync(cancellationToken));
            var successResponse = CommonResponse<InsurancePremiumDto>.Success(premiumDto, "Insurance premium added successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<bool>> RemoveInsurancePremiumAsync(Guid employeeId, Guid salaryId, Guid premiumId, CancellationToken cancellationToken = default)
        {
            var premium = await _unitOfWork.Employees.GetInsurancePremiumByIdAsync(premiumId, cancellationToken);
            if (premium == null || premium.EmployeeSalaryId != salaryId)
            {
                var notFoundResponse = CommonResponse<bool>.Fail(ResponseCodes.NotFound, "Insurance premium was not found on this salary revision.");
                return notFoundResponse;
            }

            _unitOfWork.Employees.RemoveInsurancePremium(premium);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var successResponse = CommonResponse<bool>.Success(true, "Insurance premium removed successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<DocumentPreviewDto>> GetPayslipPreviewAsync(Guid employeeId, Guid? fiscalYearId, CancellationToken cancellationToken = default)
        {
            var employee = await _unitOfWork.Employees.GetByIdAsync(employeeId, cancellationToken);
            if (employee == null)
            {
                var notFoundResponse = CommonResponse<DocumentPreviewDto>.Fail(ResponseCodes.NotFound, "Employee with id '" + employeeId + "' was not found.");
                return notFoundResponse;
            }

            var salaryHistoryResponse = await GetSalaryHistoryAsync(employeeId, cancellationToken);
            if (salaryHistoryResponse.Data == null || salaryHistoryResponse.Data.Count == 0)
            {
                var noSalaryResponse = CommonResponse<DocumentPreviewDto>.Fail(ResponseCodes.NotFound, "This employee has no salary on record yet.");
                return noSalaryResponse;
            }

            var latestSalary = salaryHistoryResponse.Data[0];

            var taxCalculationResponse = await GetCurrentSalaryTaxCalculationAsync(employeeId, fiscalYearId, cancellationToken);
            if (taxCalculationResponse.Data == null)
            {
                var taxFailureResponse = CommonResponse<DocumentPreviewDto>.Fail(taxCalculationResponse.ResponseCode, taxCalculationResponse.ResponseMessage);
                return taxFailureResponse;
            }

            var taxCalculationDto = taxCalculationResponse.Data;

            var documentTemplate = await _unitOfWork.DocumentTemplates.GetByTemplateTypeAsync(DocumentTemplateType.Payslip, cancellationToken);
            if (documentTemplate == null)
            {
                var noTemplateResponse = CommonResponse<DocumentPreviewDto>.Fail(ResponseCodes.NotFound, "No document template is configured for '" + DocumentTemplateType.Payslip + "' yet.");
                return noTemplateResponse;
            }

            var employeeName = BuildFullName(employee.FirstName, employee.MiddleName, employee.LastName);

            var placeholderValues = new Dictionary<string, string>
            {
                { "EmployeeName", employeeName },
                { "EmployeeCode", employee.EmployeeCode },
                { "JobPositionCode", employee.JobPositionCode },
                { "EffectiveFromDate", latestSalary.EffectiveFromDate.ToString("yyyy-MM-dd") },
                { "FiscalYearCode", taxCalculationDto.FiscalYearCode },
                { "GrossMonthly", taxCalculationDto.GrossMonthly.ToString("F2") },
                { "NetMonthly", taxCalculationDto.NetMonthly.ToString("F2") },
                { "GrossAnnualIncome", taxCalculationDto.TaxCalculation.GrossAnnualIncome.ToString("F2") },
                { "RetirementContributionAnnual", taxCalculationDto.TaxCalculation.RetirementContributionAnnual.ToString("F2") },
                { "RetirementExemption", taxCalculationDto.TaxCalculation.RetirementExemption.ToString("F2") },
                { "InsuranceDeduction", taxCalculationDto.TaxCalculation.InsuranceDeduction.ToString("F2") },
                { "AnnualTaxableIncome", taxCalculationDto.TaxCalculation.AnnualTaxableIncome.ToString("F2") },
                { "AnnualTax", taxCalculationDto.TaxCalculation.AnnualTax.ToString("F2") },
                { "MonthlyTax", taxCalculationDto.TaxCalculation.MonthlyTax.ToString("F2") },
                { "ComponentsRows", BuildComponentsRows(latestSalary.Components) },
                { "DeductionsRows", BuildDeductionsRows(latestSalary.Deductions) },
                { "InsurancePremiumsRows", BuildInsurancePremiumsRows(latestSalary.InsurancePremiums) },
                { "TaxBreakdownRows", BuildTaxBreakdownRows(taxCalculationDto.TaxCalculation.Breakdown) }
            };

            var renderedHtml = TemplateRenderer.Render(documentTemplate.HtmlContent, placeholderValues);

            var documentPreviewDto = new DocumentPreviewDto
            {
                TemplateType = DocumentTemplateType.Payslip,
                Html = renderedHtml
            };

            var successResponse = CommonResponse<DocumentPreviewDto>.Success(documentPreviewDto);
            return successResponse;
        }

        // Structured (non-HTML) list of the fiscal months that already have a payslip. A payslip
        // only exists once payroll for that month has actually been run AND approved (Approved
        // or Paid) -- a month with no run yet, or one still sitting in Draft, is omitted rather
        // than shown as a read-time projection (2026-07-21: projections were removed from this
        // endpoint entirely; use the Tax Details tab for a forward-looking estimate instead).
        public async Task<CommonResponse<List<PayslipSummaryDto>>> GetPayslipsAsync(Guid employeeId, Guid? fiscalYearId, CancellationToken cancellationToken = default)
        {
            var monthlyResponse = await GetMonthlySalaryTaxCalculationAsync(employeeId, fiscalYearId, cancellationToken);
            if (monthlyResponse.Data == null)
            {
                var failureResponse = CommonResponse<List<PayslipSummaryDto>>.Fail(monthlyResponse.ResponseCode, monthlyResponse.ResponseMessage);
                return failureResponse;
            }

            var persistedSlips = await _unitOfWork.PayrollRuns.GetSlipsForYearAsync(employeeId, monthlyResponse.Data.FiscalYearId, cancellationToken);
            var slipsByMonthIndex = new Dictionary<int, SalarySlip>();
            foreach (var slip in persistedSlips)
            {
                slipsByMonthIndex[slip.PayrollRun.MonthIndex] = slip;
            }

            var payslips = new List<PayslipSummaryDto>();
            foreach (var month in monthlyResponse.Data.Months)
            {
                if (!slipsByMonthIndex.TryGetValue(month.MonthIndex, out var persistedSlip))
                {
                    continue;
                }

                var persistedSummaryDto = new PayslipSummaryDto
                {
                    MonthIndex = month.MonthIndex,
                    MonthLabel = month.MonthName + "/" + monthlyResponse.Data.FiscalYearCode,
                    PeriodStartDate = persistedSlip.PeriodStartDate,
                    PeriodEndDate = persistedSlip.PeriodEndDate,
                    MonthDays = persistedSlip.MonthDays,
                    PayDays = (int)Math.Round(persistedSlip.PayDays),
                    Upl = (int)Math.Round(persistedSlip.UnpaidLeaveDays),
                    IsProjection = false
                };
                payslips.Add(persistedSummaryDto);
            }

            var successResponse = CommonResponse<List<PayslipSummaryDto>>.Success(payslips);
            return successResponse;
        }

        // Structured line-item detail behind the Payslip tab's modal. Only ever serves a
        // persisted, Approved-or-Paid SalarySlip -- the durable record, including adjustments
        // and manual edits (P8). A month with no run yet, or one still sitting in Draft, has no
        // payslip to show (2026-07-21: the read-time projection fallback was removed from this
        // endpoint; use the Tax Details tab for a forward-looking estimate instead).
        public async Task<CommonResponse<PayslipDetailDto>> GetPayslipDetailAsync(Guid employeeId, Guid fiscalYearId, int monthIndex, CancellationToken cancellationToken = default)
        {
            var employee = await _unitOfWork.Employees.GetByIdAsync(employeeId, cancellationToken);
            if (employee == null)
            {
                var notFoundResponse = CommonResponse<PayslipDetailDto>.Fail(ResponseCodes.NotFound, "Employee with id '" + employeeId + "' was not found.");
                return notFoundResponse;
            }

            var persistedSlip = await _unitOfWork.PayrollRuns.GetSlipForPeriodAsync(employeeId, fiscalYearId, monthIndex, cancellationToken);
            if (persistedSlip == null)
            {
                var noPayslipResponse = CommonResponse<PayslipDetailDto>.Fail(ResponseCodes.NotFound, "No payslip is available for that month yet -- payroll must be generated and approved first.");
                return noPayslipResponse;
            }

            var persistedDetailDto = BuildPersistedPayslipDetail(employee, persistedSlip, monthIndex);
            var successResponse = CommonResponse<PayslipDetailDto>.Success(persistedDetailDto);
            return successResponse;
        }

        // Forward-looking "what will next month's pay look like" estimate off the employee's
        // CURRENT salary structure -- never a persisted SalarySlip, so unlike GetPayslips*Async
        // it needs no Approved/Paid run to exist yet. "Next month" = the first fiscal-month row
        // (from MonthlyBreakdownCalculator, shared with the Tax Details tab so the two can never
        // disagree) whose period hasn't started yet. If today falls inside the fiscal year's last
        // month, there is no further month to forecast within it -- the caller passes the next
        // fiscal year's id explicitly, same as every other fiscalYearId-scoped endpoint here.
        public async Task<CommonResponse<SalaryForecastDto>> GetSalaryForecastAsync(Guid employeeId, Guid? fiscalYearId, CancellationToken cancellationToken = default)
        {
            var monthlyResponse = await GetMonthlySalaryTaxCalculationAsync(employeeId, fiscalYearId, cancellationToken);
            if (monthlyResponse.Data == null)
            {
                var failureResponse = CommonResponse<SalaryForecastDto>.Fail(monthlyResponse.ResponseCode, monthlyResponse.ResponseMessage);
                return failureResponse;
            }

            var now = DateTime.UtcNow;
            var nextMonthRow = monthlyResponse.Data.Months.FirstOrDefault(month => month.PeriodStartDate > now);
            if (nextMonthRow == null)
            {
                var noNextMonthResponse = CommonResponse<SalaryForecastDto>.Fail(ResponseCodes.NotFound, "Fiscal year '" + monthlyResponse.Data.FiscalYearCode + "' has no further month to forecast -- pass the next fiscal year's id.");
                return noNextMonthResponse;
            }

            var deductionLines = new List<MonthlyLineItemDto>(nextMonthRow.DeductionLines)
            {
                new MonthlyLineItemDto { Code = "TDS", Label = "TDS (income tax)", Amount = nextMonthRow.MonthTax }
            };

            var loans = await _unitOfWork.Employees.GetLoansByEmployeeIdAsync(employeeId, cancellationToken);
            if (loans.Count > 0)
            {
                var loanLabels = ConfigLabelHelper.BuildLabelMap(await _unitOfWork.Configs.GetByTypeCodeAsync(ConfigTypeCodes.DeductionType, cancellationToken));
                foreach (var loan in loans)
                {
                    if (loan.Status == LoanStatus.Approved && LoanCalculator.IsDueInPeriod(loan, nextMonthRow.PeriodStartDate))
                    {
                        deductionLines.Add(new MonthlyLineItemDto { Code = loan.LoanTypeCode, Label = ConfigLabelHelper.Resolve(loanLabels, loan.LoanTypeCode) + " EMI", Amount = loan.EmiAmount });
                    }
                }
            }

            var totalDeductions = 0m;
            foreach (var deductionLine in deductionLines)
            {
                totalDeductions += deductionLine.Amount;
            }

            var salaryForecastDto = new SalaryForecastDto
            {
                EmployeeId = employeeId,
                SalaryId = monthlyResponse.Data.SalaryId,
                FiscalYearId = monthlyResponse.Data.FiscalYearId,
                FiscalYearCode = monthlyResponse.Data.FiscalYearCode,
                MonthIndex = nextMonthRow.MonthIndex,
                MonthLabel = nextMonthRow.MonthName + "/" + monthlyResponse.Data.FiscalYearCode,
                PeriodStartDate = nextMonthRow.PeriodStartDate,
                PeriodEndDate = nextMonthRow.PeriodEndDate,
                MonthDays = nextMonthRow.MonthDays,
                IncomeLines = nextMonthRow.IncomeLines,
                GrossSalary = nextMonthRow.MonthGrossIncome,
                DeductionLines = deductionLines,
                TotalDeductions = totalDeductions,
                NetSalary = nextMonthRow.MonthGrossIncome - totalDeductions
            };

            var successResponse = CommonResponse<SalaryForecastDto>.Success(salaryForecastDto);
            return successResponse;
        }

        public async Task<CommonResponse<EmployeeLoanDto>> RequestLoanAsync(Guid employeeId, RequestLoanCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _requestLoanValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<EmployeeLoanDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var employee = await _unitOfWork.Employees.GetByIdAsync(employeeId, cancellationToken);
            if (employee == null)
            {
                var notFoundResponse = CommonResponse<EmployeeLoanDto>.Fail(ResponseCodes.NotFound, "Employee with id '" + employeeId + "' was not found.");
                return notFoundResponse;
            }

            var loan = new EmployeeLoan
            {
                EmployeeId = employeeId,
                LoanTypeCode = command.LoanTypeCode,
                PrincipalAmount = command.PrincipalAmount,
                EmiAmount = command.EmiAmount,
                RequestedDate = DateTime.UtcNow,
                StartDate = command.StartDate,
                Status = LoanStatus.PendingApproval,
                Remarks = command.Remarks,
                Employee = employee
            };

            await _unitOfWork.Employees.AddLoanAsync(loan, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var loanDto = EmployeeMapper.ToLoanDto(loan, await LoadCompensationLabelMapAsync(cancellationToken));
            var successResponse = CommonResponse<EmployeeLoanDto>.Success(loanDto, "Loan request submitted successfully.");
            return successResponse;
        }

        // Opportunistically persists Status = Closed for any Approved loan that's now fully
        // repaid -- self-healing on every read, same idempotent-on-every-call spirit as the
        // seeders, rather than needing a scheduled job.
        public async Task<CommonResponse<List<EmployeeLoanDto>>> GetLoansAsync(Guid employeeId, CancellationToken cancellationToken = default)
        {
            var employee = await _unitOfWork.Employees.GetByIdAsync(employeeId, cancellationToken);
            if (employee == null)
            {
                var notFoundResponse = CommonResponse<List<EmployeeLoanDto>>.Fail(ResponseCodes.NotFound, "Employee with id '" + employeeId + "' was not found.");
                return notFoundResponse;
            }

            var loans = await _unitOfWork.Employees.GetLoansByEmployeeIdAsync(employeeId, cancellationToken);
            var compensationLabels = await LoadCompensationLabelMapAsync(cancellationToken);

            var loanDtos = new List<EmployeeLoanDto>();
            var needsSave = false;
            foreach (var loan in loans)
            {
                if (loan.Status == LoanStatus.Approved && LoanCalculator.ComputeAmountRepaid(loan, DateTime.UtcNow) >= loan.PrincipalAmount)
                {
                    loan.Status = LoanStatus.Closed;
                    needsSave = true;
                }

                loanDtos.Add(EmployeeMapper.ToLoanDto(loan, compensationLabels));
            }

            if (needsSave)
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            var successResponse = CommonResponse<List<EmployeeLoanDto>>.Success(loanDtos);
            return successResponse;
        }

        public async Task<CommonResponse<EmployeeLoanDto>> ApproveLoanAsync(Guid employeeId, Guid loanId, LoanRemarksCommand command, CancellationToken cancellationToken = default)
        {
            var loan = await _unitOfWork.Employees.GetLoanByIdAsync(loanId, cancellationToken);
            if (loan == null || loan.EmployeeId != employeeId)
            {
                var notFoundResponse = CommonResponse<EmployeeLoanDto>.Fail(ResponseCodes.NotFound, "Loan was not found on this employee.");
                return notFoundResponse;
            }

            if (loan.Status != LoanStatus.PendingApproval)
            {
                var conflictResponse = CommonResponse<EmployeeLoanDto>.Fail(ResponseCodes.Conflict, "Only a pending loan request can be approved.");
                return conflictResponse;
            }

            loan.Status = LoanStatus.Approved;
            if (command != null && !string.IsNullOrWhiteSpace(command.Remarks))
            {
                loan.Remarks = command.Remarks;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var loanDto = EmployeeMapper.ToLoanDto(loan, await LoadCompensationLabelMapAsync(cancellationToken));
            var successResponse = CommonResponse<EmployeeLoanDto>.Success(loanDto, "Loan approved successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<EmployeeLoanDto>> RejectLoanAsync(Guid employeeId, Guid loanId, LoanRemarksCommand command, CancellationToken cancellationToken = default)
        {
            var loan = await _unitOfWork.Employees.GetLoanByIdAsync(loanId, cancellationToken);
            if (loan == null || loan.EmployeeId != employeeId)
            {
                var notFoundResponse = CommonResponse<EmployeeLoanDto>.Fail(ResponseCodes.NotFound, "Loan was not found on this employee.");
                return notFoundResponse;
            }

            if (loan.Status != LoanStatus.PendingApproval)
            {
                var conflictResponse = CommonResponse<EmployeeLoanDto>.Fail(ResponseCodes.Conflict, "Only a pending loan request can be rejected.");
                return conflictResponse;
            }

            loan.Status = LoanStatus.Rejected;
            if (command != null && !string.IsNullOrWhiteSpace(command.Remarks))
            {
                loan.Remarks = command.Remarks;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var loanDto = EmployeeMapper.ToLoanDto(loan, await LoadCompensationLabelMapAsync(cancellationToken));
            var successResponse = CommonResponse<EmployeeLoanDto>.Success(loanDto, "Loan rejected.");
            return successResponse;
        }

        public async Task<CommonResponse<EmployeeLoanDto>> CancelLoanAsync(Guid employeeId, Guid loanId, LoanRemarksCommand command, CancellationToken cancellationToken = default)
        {
            var loan = await _unitOfWork.Employees.GetLoanByIdAsync(loanId, cancellationToken);
            if (loan == null || loan.EmployeeId != employeeId)
            {
                var notFoundResponse = CommonResponse<EmployeeLoanDto>.Fail(ResponseCodes.NotFound, "Loan was not found on this employee.");
                return notFoundResponse;
            }

            if (loan.Status != LoanStatus.PendingApproval && loan.Status != LoanStatus.Approved)
            {
                var conflictResponse = CommonResponse<EmployeeLoanDto>.Fail(ResponseCodes.Conflict, "Only a pending or approved loan can be cancelled.");
                return conflictResponse;
            }

            loan.Status = LoanStatus.Cancelled;
            if (command != null && !string.IsNullOrWhiteSpace(command.Remarks))
            {
                loan.Remarks = command.Remarks;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var loanDto = EmployeeMapper.ToLoanDto(loan, await LoadCompensationLabelMapAsync(cancellationToken));
            var successResponse = CommonResponse<EmployeeLoanDto>.Success(loanDto, "Loan cancelled.");
            return successResponse;
        }

        public async Task<CommonResponse<SalaryAdjustmentDto>> CreateSalaryAdjustmentAsync(Guid employeeId, CreateSalaryAdjustmentCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _createSalaryAdjustmentValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<SalaryAdjustmentDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var employee = await _unitOfWork.Employees.GetByIdAsync(employeeId, cancellationToken);
            if (employee == null)
            {
                var notFoundResponse = CommonResponse<SalaryAdjustmentDto>.Fail(ResponseCodes.NotFound, "Employee with id '" + employeeId + "' was not found.");
                return notFoundResponse;
            }

            var fiscalYear = await _unitOfWork.FiscalYears.GetByIdAsync(command.FiscalYearId, cancellationToken);
            if (fiscalYear == null)
            {
                var yearNotFoundResponse = CommonResponse<SalaryAdjustmentDto>.Fail(ResponseCodes.NotFound, "Fiscal year with id '" + command.FiscalYearId + "' was not found.");
                return yearNotFoundResponse;
            }

            var typeCode = command.AdjustmentTypeCode.Trim();
            var adjustmentTypeCatalogOption = await _unitOfWork.Configs.GetByTypeCodeAndCodeAsync(ConfigTypeCodes.SalaryAdjustmentType, typeCode, cancellationToken);
            if (adjustmentTypeCatalogOption == null)
            {
                var invalidTypeResponse = CommonResponse<SalaryAdjustmentDto>.Fail(ResponseCodes.ValidationError, "AdjustmentTypeCode '" + typeCode + "' is not a known salary adjustment type option.");
                return invalidTypeResponse;
            }

            var expectedCalculateType = command.Direction == AdjustmentDirection.Increase ? SalaryLineCalculateTypes.Addition : SalaryLineCalculateTypes.Deduction;
            var calculateTypeError = SalaryLineCalculationHelper.ValidateCalculateType(adjustmentTypeCatalogOption, expectedCalculateType);
            if (calculateTypeError != null)
            {
                var calculateTypeResponse = CommonResponse<SalaryAdjustmentDto>.Fail(ResponseCodes.ValidationError, calculateTypeError);
                return calculateTypeResponse;
            }

            // An adjustment for a month whose run is already past Draft can never apply (S1) --
            // reject with a pointer to the alternatives.
            var existingRun = await _unitOfWork.PayrollRuns.GetByPeriodAsync(command.FiscalYearId, command.MonthIndex, cancellationToken);
            if (existingRun != null && existingRun.Status != PayrollRunStatus.Draft)
            {
                var lockedMonthResponse = CommonResponse<SalaryAdjustmentDto>.Fail(ResponseCodes.Conflict, "That month's payroll run is already '" + existingRun.Status + "' -- enter the adjustment for a later month or cancel the run first.");
                return lockedMonthResponse;
            }

            var adjustment = new SalaryAdjustment
            {
                EmployeeId = employeeId,
                FiscalYearId = command.FiscalYearId,
                MonthIndex = command.MonthIndex,
                AdjustmentTypeCode = typeCode,
                Direction = command.Direction,
                ValueType = command.ValueType,
                Value = command.Value,
                Quantity = command.Quantity,
                Remarks = command.Remarks?.Trim(),
                Status = AdjustmentStatus.Pending,
                Employee = employee,
                FiscalYear = fiscalYear
            };

            await _unitOfWork.Employees.AddSalaryAdjustmentAsync(adjustment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var adjustmentDto = EmployeeMapper.ToSalaryAdjustmentDto(adjustment, await LoadCompensationLabelMapAsync(cancellationToken));
            var successResponse = CommonResponse<SalaryAdjustmentDto>.Success(adjustmentDto, "Salary adjustment recorded; it will apply on the next payroll run of that month (regenerate the Draft run if one already exists).");
            return successResponse;
        }

        // Bulk twin of CreateSalaryAdjustmentAsync -- one Pending adjustment per in-scope
        // employee, all in a single SaveChangesAsync (the Dashain-allowance/bonus/leave-
        // encashment-for-everyone case). Scope: explicit EmployeeIds win; otherwise every
        // payroll-eligible employee, optionally narrowed by EmployeeCategoryCode.
        public async Task<CommonResponse<BulkSalaryAdjustmentResultDto>> CreateBulkSalaryAdjustmentsAsync(CreateBulkSalaryAdjustmentCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _createBulkSalaryAdjustmentValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<BulkSalaryAdjustmentResultDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var fiscalYear = await _unitOfWork.FiscalYears.GetByIdAsync(command.FiscalYearId, cancellationToken);
            if (fiscalYear == null)
            {
                var yearNotFoundResponse = CommonResponse<BulkSalaryAdjustmentResultDto>.Fail(ResponseCodes.NotFound, "Fiscal year with id '" + command.FiscalYearId + "' was not found.");
                return yearNotFoundResponse;
            }

            var typeCode = command.AdjustmentTypeCode.Trim();
            var adjustmentTypeCatalogOption = await _unitOfWork.Configs.GetByTypeCodeAndCodeAsync(ConfigTypeCodes.SalaryAdjustmentType, typeCode, cancellationToken);
            if (adjustmentTypeCatalogOption == null)
            {
                var invalidTypeResponse = CommonResponse<BulkSalaryAdjustmentResultDto>.Fail(ResponseCodes.ValidationError, "AdjustmentTypeCode '" + typeCode + "' is not a known salary adjustment type option.");
                return invalidTypeResponse;
            }

            var expectedCalculateType = command.Direction == AdjustmentDirection.Increase ? SalaryLineCalculateTypes.Addition : SalaryLineCalculateTypes.Deduction;
            var calculateTypeError = SalaryLineCalculationHelper.ValidateCalculateType(adjustmentTypeCatalogOption, expectedCalculateType);
            if (calculateTypeError != null)
            {
                var calculateTypeResponse = CommonResponse<BulkSalaryAdjustmentResultDto>.Fail(ResponseCodes.ValidationError, calculateTypeError);
                return calculateTypeResponse;
            }

            var existingRun = await _unitOfWork.PayrollRuns.GetByPeriodAsync(command.FiscalYearId, command.MonthIndex, cancellationToken);
            if (existingRun != null && existingRun.Status != PayrollRunStatus.Draft)
            {
                var lockedMonthResponse = CommonResponse<BulkSalaryAdjustmentResultDto>.Fail(ResponseCodes.Conflict, "That month's payroll run is already '" + existingRun.Status + "' -- enter the adjustment for a later month or cancel the run first.");
                return lockedMonthResponse;
            }

            var result = new BulkSalaryAdjustmentResultDto
            {
                FiscalYearId = command.FiscalYearId,
                MonthIndex = command.MonthIndex,
                AdjustmentTypeCode = typeCode
            };

            var targetEmployees = new List<Employee>();
            if (command.EmployeeIds != null && command.EmployeeIds.Count > 0)
            {
                var seenEmployeeIds = new HashSet<Guid>();
                foreach (var employeeId in command.EmployeeIds)
                {
                    if (!seenEmployeeIds.Add(employeeId))
                    {
                        continue;
                    }

                    var employee = await _unitOfWork.Employees.GetByIdAsync(employeeId, cancellationToken);
                    if (employee == null)
                    {
                        result.Skipped.Add(new BulkSalaryAdjustmentSkipDto
                        {
                            EmployeeId = employeeId,
                            EmployeeName = null,
                            Reason = "Employee was not found."
                        });
                        continue;
                    }

                    targetEmployees.Add(employee);
                }
            }
            else
            {
                var eligibleEmployees = await _unitOfWork.Employees.GetPayrollEligibleEmployeesAsync(cancellationToken);
                var categoryFilter = command.EmployeeCategoryCode?.Trim();

                foreach (var employee in eligibleEmployees)
                {
                    if (!string.IsNullOrEmpty(categoryFilter) && employee.EmployeeCategoryCode != categoryFilter)
                    {
                        continue;
                    }

                    targetEmployees.Add(employee);
                }
            }

            if (targetEmployees.Count == 0)
            {
                var noTargetsResponse = CommonResponse<BulkSalaryAdjustmentResultDto>.Fail(ResponseCodes.ValidationError, "No employees matched the given scope.");
                return noTargetsResponse;
            }

            var compensationLabels = await LoadCompensationLabelMapAsync(cancellationToken);

            foreach (var employee in targetEmployees)
            {
                var adjustment = new SalaryAdjustment
                {
                    EmployeeId = employee.Id,
                    FiscalYearId = command.FiscalYearId,
                    MonthIndex = command.MonthIndex,
                    AdjustmentTypeCode = typeCode,
                    Direction = command.Direction,
                    ValueType = command.ValueType,
                    Value = command.Value,
                    Quantity = command.Quantity,
                    Remarks = command.Remarks?.Trim(),
                    Status = AdjustmentStatus.Pending,
                    Employee = employee,
                    FiscalYear = fiscalYear
                };

                await _unitOfWork.Employees.AddSalaryAdjustmentAsync(adjustment, cancellationToken);

                var adjustmentDto = EmployeeMapper.ToSalaryAdjustmentDto(adjustment, compensationLabels);
                result.Adjustments.Add(adjustmentDto);
                result.CreatedCount++;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var successResponse = CommonResponse<BulkSalaryAdjustmentResultDto>.Success(result, "Created " + result.CreatedCount + " Pending adjustment(s); skipped " + result.Skipped.Count + ". They will apply on that month's next payroll run (refresh/regenerate the Draft run if one already exists).");
            return successResponse;
        }

        public async Task<CommonResponse<List<SalaryAdjustmentDto>>> GetSalaryAdjustmentsAsync(Guid employeeId, Guid? fiscalYearId, int? monthIndex, AdjustmentStatus? status, CancellationToken cancellationToken = default)
        {
            var employee = await _unitOfWork.Employees.GetByIdAsync(employeeId, cancellationToken);
            if (employee == null)
            {
                var notFoundResponse = CommonResponse<List<SalaryAdjustmentDto>>.Fail(ResponseCodes.NotFound, "Employee with id '" + employeeId + "' was not found.");
                return notFoundResponse;
            }

            var adjustments = await _unitOfWork.Employees.GetSalaryAdjustmentsByFilterAsync(employeeId, fiscalYearId, monthIndex, status, cancellationToken);
            var compensationLabels = await LoadCompensationLabelMapAsync(cancellationToken);

            var adjustmentDtos = new List<SalaryAdjustmentDto>();
            foreach (var adjustment in adjustments)
            {
                var adjustmentDto = EmployeeMapper.ToSalaryAdjustmentDto(adjustment, compensationLabels);
                adjustmentDtos.Add(adjustmentDto);
            }

            var successResponse = CommonResponse<List<SalaryAdjustmentDto>>.Success(adjustmentDtos);
            return successResponse;
        }

        public async Task<CommonResponse<SalaryAdjustmentDto>> UpdateSalaryAdjustmentAsync(Guid employeeId, Guid adjustmentId, UpdateSalaryAdjustmentCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _updateSalaryAdjustmentValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<SalaryAdjustmentDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var adjustment = await _unitOfWork.Employees.GetSalaryAdjustmentByIdAsync(adjustmentId, cancellationToken);
            if (adjustment == null || adjustment.EmployeeId != employeeId)
            {
                var notFoundResponse = CommonResponse<SalaryAdjustmentDto>.Fail(ResponseCodes.NotFound, "Salary adjustment was not found on this employee.");
                return notFoundResponse;
            }

            if (adjustment.Status != AdjustmentStatus.Pending)
            {
                var lockedResponse = CommonResponse<SalaryAdjustmentDto>.Fail(ResponseCodes.Conflict, "Only Pending adjustments can be edited; this one is '" + adjustment.Status + "'.");
                return lockedResponse;
            }

            var typeCode = command.AdjustmentTypeCode.Trim();
            var adjustmentTypeCatalogOption = await _unitOfWork.Configs.GetByTypeCodeAndCodeAsync(ConfigTypeCodes.SalaryAdjustmentType, typeCode, cancellationToken);
            if (adjustmentTypeCatalogOption == null)
            {
                var invalidTypeResponse = CommonResponse<SalaryAdjustmentDto>.Fail(ResponseCodes.ValidationError, "AdjustmentTypeCode '" + typeCode + "' is not a known salary adjustment type option.");
                return invalidTypeResponse;
            }

            var expectedCalculateType = command.Direction == AdjustmentDirection.Increase ? SalaryLineCalculateTypes.Addition : SalaryLineCalculateTypes.Deduction;
            var calculateTypeError = SalaryLineCalculationHelper.ValidateCalculateType(adjustmentTypeCatalogOption, expectedCalculateType);
            if (calculateTypeError != null)
            {
                var calculateTypeResponse = CommonResponse<SalaryAdjustmentDto>.Fail(ResponseCodes.ValidationError, calculateTypeError);
                return calculateTypeResponse;
            }

            adjustment.AdjustmentTypeCode = typeCode;
            adjustment.Direction = command.Direction;
            adjustment.ValueType = command.ValueType;
            adjustment.Value = command.Value;
            adjustment.Quantity = command.Quantity;
            adjustment.Remarks = command.Remarks?.Trim();

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var adjustmentDto = EmployeeMapper.ToSalaryAdjustmentDto(adjustment, await LoadCompensationLabelMapAsync(cancellationToken));
            var successResponse = CommonResponse<SalaryAdjustmentDto>.Success(adjustmentDto, "Salary adjustment updated successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<bool>> CancelSalaryAdjustmentAsync(Guid employeeId, Guid adjustmentId, CancellationToken cancellationToken = default)
        {
            var adjustment = await _unitOfWork.Employees.GetSalaryAdjustmentByIdAsync(adjustmentId, cancellationToken);
            if (adjustment == null || adjustment.EmployeeId != employeeId)
            {
                var notFoundResponse = CommonResponse<bool>.Fail(ResponseCodes.NotFound, "Salary adjustment was not found on this employee.");
                return notFoundResponse;
            }

            if (adjustment.Status != AdjustmentStatus.Pending)
            {
                var lockedResponse = CommonResponse<bool>.Fail(ResponseCodes.Conflict, "Only Pending adjustments can be cancelled; this one is '" + adjustment.Status + "'.");
                return lockedResponse;
            }

            adjustment.Status = AdjustmentStatus.Cancelled;
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var successResponse = CommonResponse<bool>.Success(true, "Salary adjustment cancelled.");
            return successResponse;
        }

        // 2026-07-23: Qualifications and Documents moved here from TeacherService/ITeacherService
        // wholesale -- neither concept was ever actually teaching-specific (any staff member can
        // hold a degree or need an identity document on file), and there is deliberately no
        // Teacher-side alias for these anymore (per instruction: don't segregate teacher vs.
        // employee here, just use the Employee endpoints for every staff member).

        public async Task<CommonResponse<EmployeeQualificationDto>> AddQualificationAsync(Guid employeeId, AddEmployeeQualificationCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _addQualificationValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<EmployeeQualificationDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var employee = await _unitOfWork.Employees.GetByIdAsync(employeeId, cancellationToken);
            if (employee == null)
            {
                var notFoundResponse = CommonResponse<EmployeeQualificationDto>.Fail(ResponseCodes.NotFound, "Employee with id '" + employeeId + "' was not found.");
                return notFoundResponse;
            }

            var qualificationCode = command.QualificationCode.Trim();
            var qualificationCodeExists = await _unitOfWork.Configs.CodeExistsAsync(ConfigTypeCodes.EmployeeQualification, qualificationCode, cancellationToken);
            if (!qualificationCodeExists)
            {
                var invalidCodeResponse = CommonResponse<EmployeeQualificationDto>.Fail(ResponseCodes.ValidationError, "QualificationCode '" + qualificationCode + "' is not a known qualification option.");
                return invalidCodeResponse;
            }

            var qualification = new EmployeeQualification
            {
                EmployeeId = employeeId,
                QualificationCode = qualificationCode,
                CourseName = command.CourseName?.Trim(),
                Institution = command.Institution?.Trim(),
                CompletionYear = command.CompletionYear,
                Score = command.Score?.Trim(),
                Remarks = command.Remarks?.Trim()
            };

            await _unitOfWork.Employees.AddQualificationAsync(qualification, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var qualificationDto = EmployeeMapper.ToQualificationDto(qualification);
            var successResponse = CommonResponse<EmployeeQualificationDto>.Success(qualificationDto, "Qualification added successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<bool>> RemoveQualificationAsync(Guid employeeId, Guid qualificationId, CancellationToken cancellationToken = default)
        {
            var qualification = await _unitOfWork.Employees.GetQualificationByIdAsync(qualificationId, cancellationToken);
            if (qualification == null || qualification.EmployeeId != employeeId)
            {
                var notFoundResponse = CommonResponse<bool>.Fail(ResponseCodes.NotFound, "Qualification was not found on this employee.");
                return notFoundResponse;
            }

            _unitOfWork.Employees.RemoveQualification(qualification);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var successResponse = CommonResponse<bool>.Success(true, "Qualification removed successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<List<EmployeeQualificationDto>>> GetQualificationsAsync(Guid employeeId, CancellationToken cancellationToken = default)
        {
            var employee = await _unitOfWork.Employees.GetByIdAsync(employeeId, cancellationToken);
            if (employee == null)
            {
                var notFoundResponse = CommonResponse<List<EmployeeQualificationDto>>.Fail(ResponseCodes.NotFound, "Employee with id '" + employeeId + "' was not found.");
                return notFoundResponse;
            }

            var qualifications = await _unitOfWork.Employees.GetQualificationsAsync(employeeId, cancellationToken);

            var qualificationDtos = new List<EmployeeQualificationDto>();
            foreach (var qualification in qualifications)
            {
                var qualificationDto = EmployeeMapper.ToQualificationDto(qualification);
                qualificationDtos.Add(qualificationDto);
            }

            var successResponse = CommonResponse<List<EmployeeQualificationDto>>.Success(qualificationDtos);
            return successResponse;
        }

        public async Task<CommonResponse<EmployeeDocumentDto>> UploadDocumentAsync(Guid employeeId, UploadEmployeeDocumentCommand command, Stream fileContent, string originalFileName, string contentType, long fileSizeBytes, CancellationToken cancellationToken = default)
        {
            var validationResult = _uploadDocumentValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<EmployeeDocumentDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            if (fileContent == null || fileSizeBytes <= 0)
            {
                var noFileResponse = CommonResponse<EmployeeDocumentDto>.Fail(ResponseCodes.ValidationError, "A document file is required.");
                return noFileResponse;
            }

            if (!DocumentFileRules.IsAllowedExtension(originalFileName))
            {
                var extensionResponse = CommonResponse<EmployeeDocumentDto>.Fail(ResponseCodes.ValidationError, "Unsupported file type. Allowed: " + DocumentFileRules.AllowedExtensionsDisplay() + ".");
                return extensionResponse;
            }

            if (fileSizeBytes > DocumentFileRules.MaxFileSizeBytes)
            {
                var sizeResponse = CommonResponse<EmployeeDocumentDto>.Fail(ResponseCodes.ValidationError, "File exceeds the maximum size of " + (DocumentFileRules.MaxFileSizeBytes / (1024 * 1024)) + " MB.");
                return sizeResponse;
            }

            var employee = await _unitOfWork.Employees.GetByIdAsync(employeeId, cancellationToken);
            if (employee == null)
            {
                var notFoundResponse = CommonResponse<EmployeeDocumentDto>.Fail(ResponseCodes.NotFound, "Employee with id '" + employeeId + "' was not found.");
                return notFoundResponse;
            }

            var documentTypeCode = command.DocumentTypeCode.Trim();
            var typeExists = await _unitOfWork.Configs.CodeExistsAsync(ConfigTypeCodes.DocumentType, documentTypeCode, cancellationToken);
            if (!typeExists)
            {
                var typeInvalidResponse = CommonResponse<EmployeeDocumentDto>.Fail(ResponseCodes.ValidationError, "DocumentTypeCode '" + documentTypeCode + "' is not a known document type option.");
                return typeInvalidResponse;
            }

            var storedPath = await _fileStorage.SaveAsync(fileContent, originalFileName, "employee-documents/" + employeeId, cancellationToken);

            var document = new EmployeeDocument
            {
                EmployeeId = employeeId,
                DocumentTypeCode = documentTypeCode,
                DocumentName = command.DocumentName.Trim(),
                FileName = originalFileName,
                FilePath = storedPath,
                ContentType = contentType,
                FileSizeBytes = fileSizeBytes,
                ValidUntil = command.ValidUntil,
                Remarks = command.Remarks?.Trim()
            };

            await _unitOfWork.Employees.AddDocumentAsync(document, cancellationToken);
            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                // The row didn't land, so the stored file must not linger as an orphan.
                _fileStorage.Delete(storedPath);
                throw;
            }

            var documentDto = EmployeeMapper.ToDocumentDto(document);
            var successResponse = CommonResponse<EmployeeDocumentDto>.Success(documentDto, "Document uploaded successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<List<EmployeeDocumentDto>>> GetDocumentsAsync(Guid employeeId, CancellationToken cancellationToken = default)
        {
            var employee = await _unitOfWork.Employees.GetByIdAsync(employeeId, cancellationToken);
            if (employee == null)
            {
                var notFoundResponse = CommonResponse<List<EmployeeDocumentDto>>.Fail(ResponseCodes.NotFound, "Employee with id '" + employeeId + "' was not found.");
                return notFoundResponse;
            }

            var documents = await _unitOfWork.Employees.GetDocumentsAsync(employeeId, cancellationToken);

            var documentDtos = new List<EmployeeDocumentDto>();
            foreach (var document in documents)
            {
                var documentDto = EmployeeMapper.ToDocumentDto(document);
                documentDtos.Add(documentDto);
            }

            var successResponse = CommonResponse<List<EmployeeDocumentDto>>.Success(documentDtos);
            return successResponse;
        }

        public async Task<CommonResponse<EmployeeDocumentFileDto>> GetDocumentFileAsync(Guid employeeId, Guid documentId, CancellationToken cancellationToken = default)
        {
            var document = await _unitOfWork.Employees.GetDocumentByIdAsync(documentId, cancellationToken);
            if (document == null || document.EmployeeId != employeeId)
            {
                var notFoundResponse = CommonResponse<EmployeeDocumentFileDto>.Fail(ResponseCodes.NotFound, "Document was not found on this employee.");
                return notFoundResponse;
            }

            var contentStream = await _fileStorage.OpenReadAsync(document.FilePath, cancellationToken);
            if (contentStream == null)
            {
                var fileMissingResponse = CommonResponse<EmployeeDocumentFileDto>.Fail(ResponseCodes.NotFound, "The stored file for this document is missing.");
                return fileMissingResponse;
            }

            var fileDto = new EmployeeDocumentFileDto
            {
                Content = contentStream,
                ContentType = string.IsNullOrWhiteSpace(document.ContentType) ? "application/octet-stream" : document.ContentType,
                FileName = document.FileName
            };

            var successResponse = CommonResponse<EmployeeDocumentFileDto>.Success(fileDto);
            return successResponse;
        }

        public async Task<CommonResponse<bool>> DeleteDocumentAsync(Guid employeeId, Guid documentId, CancellationToken cancellationToken = default)
        {
            var document = await _unitOfWork.Employees.GetDocumentByIdAsync(documentId, cancellationToken);
            if (document == null || document.EmployeeId != employeeId)
            {
                var notFoundResponse = CommonResponse<bool>.Fail(ResponseCodes.NotFound, "Document was not found on this employee.");
                return notFoundResponse;
            }

            _unitOfWork.Employees.RemoveDocument(document);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Only after the row is gone -- a failed save must not strand a row pointing at a
            // deleted file. Best-effort by contract.
            _fileStorage.Delete(document.FilePath);

            var successResponse = CommonResponse<bool>.Success(true, "Document deleted successfully.");
            return successResponse;
        }

        // Maps a persisted SalarySlip (payroll-run redesign, 2026-07-16) onto the same
        // PayslipDetailDto shape the projection produces, so the UI needs no second model.
        private static PayslipDetailDto BuildPersistedPayslipDetail(Employee employee, SalarySlip slip, int monthIndex)
        {
            var incomeLines = new List<MonthlyLineItemDto>();
            var deductionLines = new List<MonthlyLineItemDto>();
            decimal grossIncome = 0m;
            decimal totalDeduction = 0m;

            foreach (var line in slip.Lines)
            {
                var lineItem = new MonthlyLineItemDto
                {
                    Code = string.IsNullOrWhiteSpace(line.ComponentCode) ? line.Description : line.ComponentCode,
                    // Persisted slips already carry the resolved label as the line Description
                    // (falling back to the code on slips generated before 2026-07-19).
                    Label = string.IsNullOrWhiteSpace(line.Description) ? line.ComponentCode : line.Description,
                    Amount = line.Amount
                };

                if (line.LineType == SalaryLineType.Earning)
                {
                    incomeLines.Add(lineItem);
                    grossIncome += line.Amount;
                }
                else
                {
                    deductionLines.Add(lineItem);
                    totalDeduction += line.Amount;
                }
            }

            var fiscalYearCode = slip.PayrollRun?.FiscalYear?.Code;

            var payslipDetailDto = new PayslipDetailDto
            {
                EmployeeName = BuildFullName(employee.FirstName, employee.MiddleName, employee.LastName),
                EmployeeCode = employee.EmployeeCode,
                JobPositionCode = employee.JobPositionCode,
                PayMonthLabel = MonthlyBreakdownCalculator.GetMonthName(monthIndex) + "/" + fiscalYearCode,
                MonthDays = slip.MonthDays,
                PayDays = (int)Math.Round(slip.PayDays),
                Upl = (int)Math.Round(slip.UnpaidLeaveDays),
                IncomeLines = incomeLines,
                GrossIncome = grossIncome,
                DeductionLines = deductionLines,
                TotalDeduction = totalDeduction,
                NetPaid = grossIncome - totalDeduction,
                IsProjection = false
            };

            return payslipDetailDto;
        }

        // Merged label map for every catalog a compensation-plan code can come from -- salary
        // components, deductions (also loan types), insurance types, and salary adjustment
        // types (2026-07-19). Their code namespaces are distinct by convention.
        private async Task<Dictionary<string, string>> LoadCompensationLabelMapAsync(CancellationToken cancellationToken)
        {
            var labelsByCode = ConfigLabelHelper.BuildLabelMap(await _unitOfWork.Configs.GetByTypeCodeAsync(ConfigTypeCodes.SalaryComponentType, cancellationToken));
            ConfigLabelHelper.MergeLabelMap(labelsByCode, await _unitOfWork.Configs.GetByTypeCodeAsync(ConfigTypeCodes.DeductionType, cancellationToken));
            ConfigLabelHelper.MergeLabelMap(labelsByCode, await _unitOfWork.Configs.GetByTypeCodeAsync(ConfigTypeCodes.InsuranceType, cancellationToken));
            ConfigLabelHelper.MergeLabelMap(labelsByCode, await _unitOfWork.Configs.GetByTypeCodeAsync(ConfigTypeCodes.SalaryAdjustmentType, cancellationToken));
            return labelsByCode;
        }

        // 2026-07-22, format updated 2026-07-23: merged SalaryComponentType/DeductionType
        // calculation-mode map -- every code whose AdditionalValue1 parses as the composite
        // "CALCULATE_TYPE|TYPE|FREQUENCY" rule gets an entry; a code that doesn't (blank/legacy)
        // keeps today's free-form per-line ValueType/Value/FrequencyType behavior. SalaryAdjustmentType
        // is not merged in here -- SalaryAdjustment has no percentage-base-resolution or
        // FrequencyType field for this map to drive; its own CALCULATE_TYPE is validated directly
        // against Direction where adjustments are created/updated instead.
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

        private static string BuildComponentsRows(List<SalaryComponentDto> components)
        {
            var rowsHtml = string.Empty;
            foreach (var component in components)
            {
                rowsHtml += "<tr><td>" + component.ComponentCode + "</td><td>" + component.ValueType + "</td><td>" + component.Value.ToString("F2") + "</td><td>" + component.FrequencyType + "</td></tr>";
            }

            return rowsHtml;
        }

        private static string BuildDeductionsRows(List<SalaryDeductionDto> deductions)
        {
            var rowsHtml = string.Empty;
            foreach (var deduction in deductions)
            {
                rowsHtml += "<tr><td>" + deduction.DeductionCode + "</td><td>" + deduction.ValueType + "</td><td>" + deduction.Value.ToString("F2") + "</td><td>" + deduction.FrequencyType + "</td></tr>";
            }

            return rowsHtml;
        }

        private static string BuildInsurancePremiumsRows(List<InsurancePremiumDto> insurancePremiums)
        {
            var rowsHtml = string.Empty;
            foreach (var insurancePremium in insurancePremiums)
            {
                rowsHtml += "<tr><td>" + insurancePremium.InsuranceTypeCode + "</td><td>" + insurancePremium.AnnualPremiumAmount.ToString("F2") + "</td></tr>";
            }

            return rowsHtml;
        }

        private static string BuildTaxBreakdownRows(List<TaxSlabBreakdownDto> breakdown)
        {
            var rowsHtml = string.Empty;
            foreach (var slabBreakdown in breakdown)
            {
                var maxAmountText = slabBreakdown.MaxAmount.HasValue ? slabBreakdown.MaxAmount.Value.ToString("F2") : "-";
                rowsHtml += "<tr><td>" + slabBreakdown.MinAmount.ToString("F2") + "</td><td>" + maxAmountText + "</td><td>" + slabBreakdown.TaxRate.ToString("P2") + "</td><td>" + slabBreakdown.TaxableAmountInSlab.ToString("F2") + "</td><td>" + slabBreakdown.TaxForSlab.ToString("F2") + "</td></tr>";
            }

            return rowsHtml;
        }

        private async Task<Dictionary<string, InsuranceCapConfig>> BuildInsuranceTypeCapsAsync(CancellationToken cancellationToken)
        {
            var insuranceTypes = await _unitOfWork.Configs.GetByTypeCodeAsync(ConfigTypeCodes.InsuranceType, cancellationToken);
            return InsuranceCapHelper.BuildCapMap(insuranceTypes);
        }

        // Validates every submitted component/deduction/insurance-premium code up front so a bad
        // entry fails the whole create-salary request rather than half-saving a revision.
        private async Task<string> ValidateLineItemCodesAsync(List<SalaryComponentInput> components, List<SalaryDeductionInput> deductions, List<InsurancePremiumInput> premiums, CancellationToken cancellationToken)
        {
            foreach (var component in components)
            {
                var componentCatalogOption = await _unitOfWork.Configs.GetByTypeCodeAndCodeAsync(ConfigTypeCodes.SalaryComponentType, component.ComponentCode.Trim(), cancellationToken);
                if (componentCatalogOption == null)
                {
                    return "ComponentCode '" + component.ComponentCode + "' is not a known salary component option.";
                }

                var percentageLockError = SalaryLineCalculationHelper.ValidatePercentageLock(componentCatalogOption, component.ValueType, component.Value);
                if (percentageLockError != null)
                {
                    return percentageLockError;
                }

                var componentCalculateTypeError = SalaryLineCalculationHelper.ValidateCalculateType(componentCatalogOption, SalaryLineCalculateTypes.Addition);
                if (componentCalculateTypeError != null)
                {
                    return componentCalculateTypeError;
                }

                var componentFrequencyLockError = SalaryLineCalculationHelper.ValidateFrequencyLock(componentCatalogOption, component.FrequencyType);
                if (componentFrequencyLockError != null)
                {
                    return componentFrequencyLockError;
                }
            }

            foreach (var deduction in deductions)
            {
                var deductionCatalogOption = await _unitOfWork.Configs.GetByTypeCodeAndCodeAsync(ConfigTypeCodes.DeductionType, deduction.DeductionCode.Trim(), cancellationToken);
                if (deductionCatalogOption == null)
                {
                    return "DeductionCode '" + deduction.DeductionCode + "' is not a known deduction option.";
                }

                var deductionPercentageLockError = SalaryLineCalculationHelper.ValidatePercentageLock(deductionCatalogOption, deduction.ValueType, deduction.Value);
                if (deductionPercentageLockError != null)
                {
                    return deductionPercentageLockError;
                }

                var deductionCalculateTypeError = SalaryLineCalculationHelper.ValidateCalculateType(deductionCatalogOption, SalaryLineCalculateTypes.Deduction);
                if (deductionCalculateTypeError != null)
                {
                    return deductionCalculateTypeError;
                }

                var deductionFrequencyLockError = SalaryLineCalculationHelper.ValidateFrequencyLock(deductionCatalogOption, deduction.FrequencyType);
                if (deductionFrequencyLockError != null)
                {
                    return deductionFrequencyLockError;
                }
            }

            foreach (var premium in premiums)
            {
                var exists = await _unitOfWork.Configs.CodeExistsAsync(ConfigTypeCodes.InsuranceType, premium.InsuranceTypeCode.Trim(), cancellationToken);
                if (!exists)
                {
                    return "InsuranceTypeCode '" + premium.InsuranceTypeCode + "' is not a known insurance type option.";
                }
            }

            return null;
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
