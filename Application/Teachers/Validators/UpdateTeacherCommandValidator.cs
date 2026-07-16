using Application.Teachers.Commands;
using Domain.Constants;
using FluentValidation;

namespace Application.Teachers.Validators
{
    public class UpdateTeacherCommandValidator : AbstractValidator<UpdateTeacherCommand>
    {
        public UpdateTeacherCommandValidator()
        {
            RuleFor(command => command.FirstName)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(command => command.MiddleName)
                .MaximumLength(100);

            RuleFor(command => command.LastName)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(command => command.Email)
                .EmailAddress()
                .MaximumLength(255)
                .When(command => !string.IsNullOrWhiteSpace(command.Email));

            RuleFor(command => command.Phone)
                .MaximumLength(20);

            RuleFor(command => command.JobPositionCode)
                .NotEmpty()
                .MaximumLength(100)
                .Must(code => code == JobPositionCodes.Teacher || code == JobPositionCodes.Principal || code == JobPositionCodes.VicePrincipal)
                    .WithMessage("JobPositionCode must be one of: " + JobPositionCodes.Teacher + ", " + JobPositionCodes.Principal + ", " + JobPositionCodes.VicePrincipal + ".")
                    .When(command => !string.IsNullOrWhiteSpace(command.JobPositionCode));

            RuleFor(command => command.JoinDate)
                .GreaterThanOrEqualTo(command => command.DateOfBirth)
                    .WithMessage("JoinDate cannot be before DateOfBirth.")
                .When(command => command.JoinDate.HasValue && command.DateOfBirth.HasValue);

            RuleFor(command => command.Status)
                .IsInEnum();

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
