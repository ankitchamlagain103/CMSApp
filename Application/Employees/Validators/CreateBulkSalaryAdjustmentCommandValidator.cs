using Application.Employees.Commands;
using FluentValidation;

namespace Application.Employees.Validators
{
    public class CreateBulkSalaryAdjustmentCommandValidator : AbstractValidator<CreateBulkSalaryAdjustmentCommand>
    {
        public CreateBulkSalaryAdjustmentCommandValidator()
        {
            Include(new CreateSalaryAdjustmentCommandValidator());

            RuleFor(command => command.EmployeeCategoryCode)
                .MaximumLength(100);

            RuleFor(command => command.EmployeeIds)
                .Must(HaveNoEmptyIds)
                .WithMessage("EmployeeIds must not contain an empty Guid.");
        }

        private static bool HaveNoEmptyIds(List<Guid> employeeIds)
        {
            if (employeeIds == null)
            {
                return true;
            }

            foreach (var employeeId in employeeIds)
            {
                if (employeeId == Guid.Empty)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
