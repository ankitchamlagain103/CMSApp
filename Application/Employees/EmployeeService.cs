using Application.Common.Helpers;
using Application.Common.Interfaces;
using Application.Common.Models;
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
        private readonly CreateEmployeeCommandValidator _createValidator;
        private readonly UpdateEmployeeCommandValidator _updateValidator;
        private readonly PromoteToTeacherCommandValidator _promoteValidator;
        private readonly AddEmployeeSalaryCommandValidator _addSalaryValidator;
        private readonly SalaryComponentInputValidator _componentValidator;
        private readonly SalaryDeductionInputValidator _deductionValidator;
        private readonly InsurancePremiumInputValidator _premiumValidator;
        private readonly RequestLoanCommandValidator _requestLoanValidator;

        public EmployeeService(
            IUnitOfWork unitOfWork,
            CreateEmployeeCommandValidator createValidator,
            UpdateEmployeeCommandValidator updateValidator,
            PromoteToTeacherCommandValidator promoteValidator,
            AddEmployeeSalaryCommandValidator addSalaryValidator,
            SalaryComponentInputValidator componentValidator,
            SalaryDeductionInputValidator deductionValidator,
            InsurancePremiumInputValidator premiumValidator,
            RequestLoanCommandValidator requestLoanValidator)
        {
            _unitOfWork = unitOfWork;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _promoteValidator = promoteValidator;
            _addSalaryValidator = addSalaryValidator;
            _componentValidator = componentValidator;
            _deductionValidator = deductionValidator;
            _premiumValidator = premiumValidator;
            _requestLoanValidator = requestLoanValidator;
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
                PaymentMode = command.PaymentMode
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

            var salaryDto = EmployeeMapper.ToSalaryDto(salary);
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

            var salaryDtos = new List<EmployeeSalaryDto>();
            foreach (var salary in salaries)
            {
                var fullSalary = await _unitOfWork.Employees.GetSalaryWithLineItemsAsync(salary.Id, cancellationToken);
                var salaryDto = EmployeeMapper.ToSalaryDto(fullSalary);
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

            var taxCalculation = TaxCalculator.CalculateFromSalary(
                currentSalary.Components.ToList(),
                currentSalary.Deductions.ToList(),
                currentSalary.InsurancePremiums.ToList(),
                insuranceTypeCaps,
                fiscalYear.RetirementExemptionCapAmount,
                taxSlabs);

            var taxCalculationDto = new EmployeeTaxCalculationDto
            {
                EmployeeId = employeeId,
                SalaryId = currentSalary.Id,
                FiscalYearId = fiscalYear.Id,
                FiscalYearCode = fiscalYear.Code,
                GrossMonthly = taxCalculation.GrossAnnualIncome / 12m,
                TaxCalculation = taxCalculation,
                NetMonthly = (taxCalculation.GrossAnnualIncome / 12m) - taxCalculation.MonthlyTax
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

            var months = MonthlyBreakdownCalculator.Build(
                currentSalary.Components.ToList(),
                currentSalary.Deductions.ToList(),
                currentSalary.EffectiveFromDate,
                fiscalYear,
                taxCalculationDto.TaxCalculation.AnnualTax);

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
            var codeExists = await _unitOfWork.Configs.CodeExistsAsync(ConfigTypeCodes.SalaryComponentType, componentCode, cancellationToken);
            if (!codeExists)
            {
                var invalidCodeResponse = CommonResponse<SalaryComponentDto>.Fail(ResponseCodes.ValidationError, "ComponentCode '" + componentCode + "' is not a known salary component option.");
                return invalidCodeResponse;
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

            var componentDto = EmployeeMapper.ToComponentDto(component);
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
            var codeExists = await _unitOfWork.Configs.CodeExistsAsync(ConfigTypeCodes.DeductionType, deductionCode, cancellationToken);
            if (!codeExists)
            {
                var invalidCodeResponse = CommonResponse<SalaryDeductionDto>.Fail(ResponseCodes.ValidationError, "DeductionCode '" + deductionCode + "' is not a known deduction option.");
                return invalidCodeResponse;
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

            var deductionDto = EmployeeMapper.ToDeductionDto(deduction);
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

            var premiumDto = EmployeeMapper.ToInsurancePremiumDto(premium);
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

        // Structured (non-HTML) list of the fiscal months that already have a payslip -- a month
        // whose pay period hasn't started yet is omitted, matching a real payslip only existing
        // once its period has begun.
        public async Task<CommonResponse<List<PayslipSummaryDto>>> GetPayslipsAsync(Guid employeeId, Guid? fiscalYearId, CancellationToken cancellationToken = default)
        {
            var monthlyResponse = await GetMonthlySalaryTaxCalculationAsync(employeeId, fiscalYearId, cancellationToken);
            if (monthlyResponse.Data == null)
            {
                var failureResponse = CommonResponse<List<PayslipSummaryDto>>.Fail(monthlyResponse.ResponseCode, monthlyResponse.ResponseMessage);
                return failureResponse;
            }

            var now = DateTime.UtcNow;
            var payslips = new List<PayslipSummaryDto>();
            foreach (var month in monthlyResponse.Data.Months)
            {
                if (month.PeriodStartDate > now)
                {
                    continue;
                }

                var payslipSummaryDto = new PayslipSummaryDto
                {
                    MonthIndex = month.MonthIndex,
                    MonthLabel = month.MonthName + "/" + monthlyResponse.Data.FiscalYearCode,
                    PeriodStartDate = month.PeriodStartDate,
                    PeriodEndDate = month.PeriodEndDate,
                    MonthDays = month.MonthDays,
                    PayDays = month.MonthDays,
                    Upl = 0
                };
                payslips.Add(payslipSummaryDto);
            }

            var successResponse = CommonResponse<List<PayslipSummaryDto>>.Success(payslips);
            return successResponse;
        }

        // Structured line-item detail behind the Payslip tab's modal. Folds in that month's flat
        // TDS share and any Approved, not-yet-fully-repaid EmployeeLoan due that month as extra
        // deduction lines -- the "auto-deduction" described on EmployeeLoan, computed at read time
        // rather than a persisted EmployeeSalaryDeduction row.
        public async Task<CommonResponse<PayslipDetailDto>> GetPayslipDetailAsync(Guid employeeId, Guid fiscalYearId, int monthIndex, CancellationToken cancellationToken = default)
        {
            var employee = await _unitOfWork.Employees.GetByIdAsync(employeeId, cancellationToken);
            if (employee == null)
            {
                var notFoundResponse = CommonResponse<PayslipDetailDto>.Fail(ResponseCodes.NotFound, "Employee with id '" + employeeId + "' was not found.");
                return notFoundResponse;
            }

            var monthlyResponse = await GetMonthlySalaryTaxCalculationAsync(employeeId, fiscalYearId, cancellationToken);
            if (monthlyResponse.Data == null)
            {
                var failureResponse = CommonResponse<PayslipDetailDto>.Fail(monthlyResponse.ResponseCode, monthlyResponse.ResponseMessage);
                return failureResponse;
            }

            var monthRow = monthlyResponse.Data.Months.FirstOrDefault(month => month.MonthIndex == monthIndex);
            if (monthRow == null || monthRow.PeriodStartDate > DateTime.UtcNow)
            {
                var noPayslipResponse = CommonResponse<PayslipDetailDto>.Fail(ResponseCodes.NotFound, "No payslip is available for that month yet.");
                return noPayslipResponse;
            }

            var deductionLines = new List<MonthlyLineItemDto>(monthRow.DeductionLines)
            {
                new MonthlyLineItemDto { Code = "TDS", Amount = monthRow.MonthTax }
            };

            var loans = await _unitOfWork.Employees.GetLoansByEmployeeIdAsync(employeeId, cancellationToken);
            foreach (var loan in loans)
            {
                if (loan.Status == LoanStatus.Approved && LoanCalculator.IsDueInPeriod(loan, monthRow.PeriodStartDate))
                {
                    deductionLines.Add(new MonthlyLineItemDto { Code = loan.LoanTypeCode, Amount = loan.EmiAmount });
                }
            }

            var totalDeduction = 0m;
            foreach (var deductionLine in deductionLines)
            {
                totalDeduction += deductionLine.Amount;
            }

            var payslipDetailDto = new PayslipDetailDto
            {
                EmployeeName = BuildFullName(employee.FirstName, employee.MiddleName, employee.LastName),
                EmployeeCode = employee.EmployeeCode,
                JobPositionCode = employee.JobPositionCode,
                PayMonthLabel = monthRow.MonthName + "/" + monthlyResponse.Data.FiscalYearCode,
                MonthDays = monthRow.MonthDays,
                PayDays = monthRow.MonthDays,
                Upl = 0,
                IncomeLines = monthRow.IncomeLines,
                GrossIncome = monthRow.MonthGrossIncome,
                DeductionLines = deductionLines,
                TotalDeduction = totalDeduction,
                NetPaid = monthRow.MonthGrossIncome - totalDeduction
            };

            var successResponse = CommonResponse<PayslipDetailDto>.Success(payslipDetailDto);
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

            var loanDto = EmployeeMapper.ToLoanDto(loan);
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

            var loanDtos = new List<EmployeeLoanDto>();
            var needsSave = false;
            foreach (var loan in loans)
            {
                if (loan.Status == LoanStatus.Approved && LoanCalculator.ComputeAmountRepaid(loan, DateTime.UtcNow) >= loan.PrincipalAmount)
                {
                    loan.Status = LoanStatus.Closed;
                    needsSave = true;
                }

                loanDtos.Add(EmployeeMapper.ToLoanDto(loan));
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

            var loanDto = EmployeeMapper.ToLoanDto(loan);
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

            var loanDto = EmployeeMapper.ToLoanDto(loan);
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

            var loanDto = EmployeeMapper.ToLoanDto(loan);
            var successResponse = CommonResponse<EmployeeLoanDto>.Success(loanDto, "Loan cancelled.");
            return successResponse;
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

        private async Task<Dictionary<string, decimal>> BuildInsuranceTypeCapsAsync(CancellationToken cancellationToken)
        {
            var insuranceTypes = await _unitOfWork.Configs.GetByTypeCodeAsync(ConfigTypeCodes.InsuranceType, cancellationToken);

            var caps = new Dictionary<string, decimal>();
            foreach (var insuranceType in insuranceTypes)
            {
                if (decimal.TryParse(insuranceType.AdditionalValue1, out var cap))
                {
                    caps[insuranceType.Code] = cap;
                }
            }

            return caps;
        }

        // Validates every submitted component/deduction/insurance-premium code up front so a bad
        // entry fails the whole create-salary request rather than half-saving a revision.
        private async Task<string> ValidateLineItemCodesAsync(List<SalaryComponentInput> components, List<SalaryDeductionInput> deductions, List<InsurancePremiumInput> premiums, CancellationToken cancellationToken)
        {
            foreach (var component in components)
            {
                var exists = await _unitOfWork.Configs.CodeExistsAsync(ConfigTypeCodes.SalaryComponentType, component.ComponentCode.Trim(), cancellationToken);
                if (!exists)
                {
                    return "ComponentCode '" + component.ComponentCode + "' is not a known salary component option.";
                }
            }

            foreach (var deduction in deductions)
            {
                var exists = await _unitOfWork.Configs.CodeExistsAsync(ConfigTypeCodes.DeductionType, deduction.DeductionCode.Trim(), cancellationToken);
                if (!exists)
                {
                    return "DeductionCode '" + deduction.DeductionCode + "' is not a known deduction option.";
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
