using Application.Employees.Commands;
using FluentValidation;

namespace Application.Employees.Validators
{
    public class UpdateEmployeeCommandValidator : AbstractValidator<UpdateEmployeeCommand>
    {
        public UpdateEmployeeCommandValidator()
        {
            RuleFor(command => command.FirstName)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(command => command.LastName)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(command => command.EmployeeCategoryCode)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(command => command.JobPositionCode)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(command => command.PanNumber)
                .MaximumLength(50);

            RuleFor(command => command.ProvidentFundNumber)
                .MaximumLength(50);

            RuleFor(command => command.SsfNumber)
                .MaximumLength(50);

            RuleFor(command => command.CitNumber)
                .MaximumLength(50);

            RuleFor(command => command.GratuityNumber)
                .MaximumLength(50);

            RuleFor(command => command.JoinDate)
                .GreaterThanOrEqualTo(command => command.DateOfBirth)
                    .WithMessage("JoinDate cannot be before DateOfBirth.")
                .When(command => command.JoinDate.HasValue && command.DateOfBirth.HasValue);
        }
    }
}
