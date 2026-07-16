using Application.Employees.Commands;
using FluentValidation;

namespace Application.Employees.Validators
{
    public class PromoteToTeacherCommandValidator : AbstractValidator<PromoteToTeacherCommand>
    {
        public PromoteToTeacherCommandValidator()
        {
            RuleFor(command => command.TeachingLicenseNo)
                .MaximumLength(100);

            RuleFor(command => command.ExperienceYears)
                .GreaterThanOrEqualTo(0)
                .When(command => command.ExperienceYears.HasValue);

            RuleFor(command => command.Specialization)
                .MaximumLength(255);
        }
    }
}
