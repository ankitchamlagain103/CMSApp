using Application.Employees.Commands;
using FluentValidation;

namespace Application.Employees.Validators
{
    public class CreateEmployeeCommandValidator : AbstractValidator<CreateEmployeeCommand>
    {
        public CreateEmployeeCommandValidator()
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

            RuleFor(command => command.EmployeeCode)
                .MaximumLength(30);

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

            // Portal account provisioning (2026-07-27) -- only required when opted in.
            RuleFor(command => command.Email)
                .NotEmpty()
                .EmailAddress()
                .When(command => command.RegisterUserAccount);

            RuleFor(command => command.RoleIds)
                .Must(roleIds => roleIds != null && roleIds.Count > 0)
                    .WithMessage("At least one role is required to create a portal account.")
                .When(command => command.RegisterUserAccount);
        }
    }
}
