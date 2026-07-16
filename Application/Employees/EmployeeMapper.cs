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
                HasTeacherProfile = hasTeacherProfile || employee.Teacher != null
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
        public static EmployeeSalaryDto ToSalaryDto(EmployeeSalary salary)
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
                salaryDto.Components.Add(ToComponentDto(component));
            }

            foreach (var deduction in salary.Deductions)
            {
                salaryDto.Deductions.Add(ToDeductionDto(deduction));
            }

            foreach (var premium in salary.InsurancePremiums)
            {
                salaryDto.InsurancePremiums.Add(ToInsurancePremiumDto(premium));
            }

            return salaryDto;
        }

        public static SalaryComponentDto ToComponentDto(EmployeeSalaryComponent component)
        {
            var componentDto = new SalaryComponentDto
            {
                Id = component.Id,
                EmployeeSalaryId = component.EmployeeSalaryId,
                ComponentCode = component.ComponentCode,
                ValueType = component.ValueType,
                Value = component.Value,
                FrequencyType = component.FrequencyType,
                IsTaxable = component.IsTaxable,
                IsRetirementContribution = component.IsRetirementContribution
            };

            return componentDto;
        }

        public static SalaryDeductionDto ToDeductionDto(EmployeeSalaryDeduction deduction)
        {
            var deductionDto = new SalaryDeductionDto
            {
                Id = deduction.Id,
                EmployeeSalaryId = deduction.EmployeeSalaryId,
                DeductionCode = deduction.DeductionCode,
                ValueType = deduction.ValueType,
                Value = deduction.Value,
                FrequencyType = deduction.FrequencyType,
                IsRetirementContribution = deduction.IsRetirementContribution
            };

            return deductionDto;
        }

        public static InsurancePremiumDto ToInsurancePremiumDto(EmployeeInsurancePremium premium)
        {
            var premiumDto = new InsurancePremiumDto
            {
                Id = premium.Id,
                EmployeeSalaryId = premium.EmployeeSalaryId,
                InsuranceTypeCode = premium.InsuranceTypeCode,
                AnnualPremiumAmount = premium.AnnualPremiumAmount
            };

            return premiumDto;
        }

        public static EmployeeLoanDto ToLoanDto(EmployeeLoan loan)
        {
            var asOfDate = DateTime.UtcNow;
            var amountRepaid = LoanCalculator.ComputeAmountRepaid(loan, asOfDate);

            var loanDto = new EmployeeLoanDto
            {
                Id = loan.Id,
                EmployeeId = loan.EmployeeId,
                LoanTypeCode = loan.LoanTypeCode,
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
    }
}
