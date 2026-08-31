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

            RuleFor(command => command.BranchCode)
                .MaximumLength(100);

            RuleFor(command => command.ProvinceCode)
                .MaximumLength(100);

            RuleFor(command => command.LevelCode)
                .MaximumLength(100);

            RuleFor(command => command.DistrictCode)
                .MaximumLength(100);

            RuleFor(command => command.LocalLevelCode)
                .MaximumLength(100);

            RuleFor(command => command.WardNo)
                .InclusiveBetween(1, 99)
                .When(command => command.WardNo.HasValue);

            RuleFor(command => command.JoinDate)
                .GreaterThanOrEqualTo(command => command.DateOfBirth)
                    .WithMessage("JoinDate cannot be before DateOfBirth.")
                .When(command => command.JoinDate.HasValue && command.DateOfBirth.HasValue);
        }
    }
}
