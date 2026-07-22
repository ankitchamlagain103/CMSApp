using Application.Common.Helpers;
using Application.Employees.Dtos;
using Application.Payroll;
using Domain.Entities;

namespace Application.Employees
{
    public static class EmployeeMapper
    {
        // HasTeacherProfile is only meaningful when the Teacher navigation was loaded
        // (GetByIdWithTeacherAsync); the plain paged list leaves it false rather than issuing a
        // per-row lookup.
        public static EmployeeDto ToDto(Employee employee, bool hasTeacherProfile = false)
        {
            var employeeDto = new EmployeeDto
            {
                Id = employee.Id,
                UserId = employee.UserId,
                EmployeeCode = employee.EmployeeCode,
                FirstName = employee.FirstName,
                MiddleName = employee.MiddleName,
                LastName = employee.LastName,
                Gender = employee.Gender,
                DateOfBirth = employee.DateOfBirth,
                Email = employee.Email,
                Phone = employee.Phone,
                JoinDate = employee.JoinDate,
                EmployeeCategoryCode = employee.EmployeeCategoryCode,
                JobPositionCode = employee.JobPositionCode,
                EmploymentStatus = employee.EmploymentStatus,
                BankName = employee.BankName,
                BankAccountNumber = employee.BankAccountNumber,
                PaymentMode = employee.PaymentMode,
                HasTeacherProfile = hasTeacherProfile || employee.Teacher != null,
                CreatedBy = employee.CreatedBy,
                CreatedTs = employee.CreatedTs,
                UpdatedBy = employee.UpdatedBy,
                UpdatedTs = employee.UpdatedTs
            };

            return employeeDto;
        }

        public static TeacherProfileDto ToTeacherProfileDto(Teacher teacher)
        {
            var teacherProfileDto = new TeacherProfileDto
            {
                EmployeeId = teacher.Id,
                TeachingLicenseNo = teacher.TeachingLicenseNo,
                ExperienceYears = teacher.ExperienceYears,
                Specialization = teacher.Specialization
            };

            return teacherProfileDto;
        }

        // Expects the salary's Components/Deductions/InsurancePremiums navigations to be loaded.
        // labelsByCode (2026-07-19): merged SalaryComponentType/DeductionType/InsuranceType
        // Config label map; null keeps every *Label field at its code.
        public static EmployeeSalaryDto ToSalaryDto(EmployeeSalary salary, IReadOnlyDictionary<string, string> labelsByCode = null)
        {
            var salaryDto = new EmployeeSalaryDto
            {
                Id = salary.Id,
                EmployeeId = salary.EmployeeId,
                EffectiveFromDate = salary.EffectiveFromDate,
                AssessmentType = salary.AssessmentType
            };

            foreach (var component in salary.Components)
            {
                salaryDto.Components.Add(ToComponentDto(component, labelsByCode));
            }

            foreach (var deduction in salary.Deductions)
            {
                salaryDto.Deductions.Add(ToDeductionDto(deduction, labelsByCode));
            }

            foreach (var premium in salary.InsurancePremiums)
            {
                salaryDto.InsurancePremiums.Add(ToInsurancePremiumDto(premium, labelsByCode));
            }

            return salaryDto;
        }

        public static SalaryComponentDto ToComponentDto(EmployeeSalaryComponent component, IReadOnlyDictionary<string, string> labelsByCode = null)
        {
            var componentDto = new SalaryComponentDto
            {
                Id = component.Id,
                EmployeeSalaryId = component.EmployeeSalaryId,
                ComponentCode = component.ComponentCode,
                ComponentLabel = ConfigLabelHelper.Resolve(labelsByCode, component.ComponentCode),
                ValueType = component.ValueType,
                Value = component.Value,
                FrequencyType = component.FrequencyType,
                IsTaxable = component.IsTaxable,
                IsRetirementContribution = component.IsRetirementContribution
            };

            return componentDto;
        }

        public static SalaryDeductionDto ToDeductionDto(EmployeeSalaryDeduction deduction, IReadOnlyDictionary<string, string> labelsByCode = null)
        {
            var deductionDto = new SalaryDeductionDto
            {
                Id = deduction.Id,
                EmployeeSalaryId = deduction.EmployeeSalaryId,
                DeductionCode = deduction.DeductionCode,
                DeductionLabel = ConfigLabelHelper.Resolve(labelsByCode, deduction.DeductionCode),
                ValueType = deduction.ValueType,
                Value = deduction.Value,
                FrequencyType = deduction.FrequencyType,
                IsRetirementContribution = deduction.IsRetirementContribution
            };

            return deductionDto;
        }

        public static InsurancePremiumDto ToInsurancePremiumDto(EmployeeInsurancePremium premium, IReadOnlyDictionary<string, string> labelsByCode = null)
        {
            var premiumDto = new InsurancePremiumDto
            {
                Id = premium.Id,
                EmployeeSalaryId = premium.EmployeeSalaryId,
                InsuranceTypeCode = premium.InsuranceTypeCode,
                InsuranceTypeLabel = ConfigLabelHelper.Resolve(labelsByCode, premium.InsuranceTypeCode),
                AnnualPremiumAmount = premium.AnnualPremiumAmount
            };

            return premiumDto;
        }

        public static EmployeeLoanDto ToLoanDto(EmployeeLoan loan, IReadOnlyDictionary<string, string> labelsByCode = null)
        {
            var asOfDate = DateTime.UtcNow;
            var amountRepaid = LoanCalculator.ComputeAmountRepaid(loan, asOfDate);

            var loanDto = new EmployeeLoanDto
            {
                Id = loan.Id,
                EmployeeId = loan.EmployeeId,
                LoanTypeCode = loan.LoanTypeCode,
                LoanTypeLabel = ConfigLabelHelper.Resolve(labelsByCode, loan.LoanTypeCode),
                PrincipalAmount = loan.PrincipalAmount,
                EmiAmount = loan.EmiAmount,
                RequestedDate = loan.RequestedDate,
                StartDate = loan.StartDate,
                Status = loan.Status,
                Remarks = loan.Remarks,
                AmountRepaid = amountRepaid,
                RemainingBalance = loan.PrincipalAmount - amountRepaid,
                IsFullyRepaid = amountRepaid >= loan.PrincipalAmount
            };

            return loanDto;
        }

        public static SalaryAdjustmentDto ToSalaryAdjustmentDto(SalaryAdjustment adjustment, IReadOnlyDictionary<string, string> labelsByCode = null)
        {
            var adjustmentDto = new SalaryAdjustmentDto
            {
                Id = adjustment.Id,
                EmployeeId = adjustment.EmployeeId,
                FiscalYearId = adjustment.FiscalYearId,
                MonthIndex = adjustment.MonthIndex,
                AdjustmentTypeCode = adjustment.AdjustmentTypeCode,
                AdjustmentTypeLabel = ConfigLabelHelper.Resolve(labelsByCode, adjustment.AdjustmentTypeCode),
                Direction = adjustment.Direction,
                ValueType = adjustment.ValueType,
                Value = adjustment.Value,
                Quantity = adjustment.Quantity,
                Remarks = adjustment.Remarks,
                Status = adjustment.Status,
                AppliedSalarySlipId = adjustment.AppliedSalarySlipId
            };

            return adjustmentDto;
        }
    }
}
