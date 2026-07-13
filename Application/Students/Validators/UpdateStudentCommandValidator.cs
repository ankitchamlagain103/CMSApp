using Application.Students.Commands;
using FluentValidation;

namespace Application.Students.Validators
{
    public class UpdateStudentCommandValidator : AbstractValidator<UpdateStudentCommand>
    {
        public UpdateStudentCommandValidator()
        {
            RuleFor(command => command.FirstName)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(command => command.MiddleName)
                .MaximumLength(100);

            RuleFor(command => command.LastName)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(command => command.Gender)
                .IsInEnum();

            RuleFor(command => command.DateOfBirth)
                .LessThan(command => DateTime.UtcNow.Date)
                    .WithMessage("DateOfBirth must be in the past.")
                .When(command => command.DateOfBirth.HasValue);

            RuleFor(command => command.Email)
                .EmailAddress()
                .MaximumLength(255)
                .When(command => !string.IsNullOrEmpty(command.Email));

            RuleFor(command => command.Phone)
                .MaximumLength(20);

            RuleFor(command => command.Address)
                .MaximumLength(500);

            RuleFor(command => command.Status)
                .IsInEnum();

            RuleForEach(command => command.Guardians)
                .SetValidator(new StudentGuardianInputValidator());

            RuleFor(command => command.Guardians)
                .Must(HaveAtMostOnePrimary)
                    .WithMessage("Only one guardian can be marked as primary.");
        }

        private static bool HaveAtMostOnePrimary(List<StudentGuardianInput> guardians)
        {
            if (guardians == null)
            {
                return true;
            }

            var primaryCount = 0;
            foreach (var guardian in guardians)
            {
                if (guardian.IsPrimary)
                {
                    primaryCount++;
                }
            }

            return primaryCount <= 1;
        }
    }
}
