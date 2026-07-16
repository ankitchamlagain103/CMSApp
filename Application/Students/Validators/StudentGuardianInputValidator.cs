using Application.Students.Commands;
using FluentValidation;

namespace Application.Students.Validators
{
    public class StudentGuardianInputValidator : AbstractValidator<StudentGuardianInput>
    {
        public StudentGuardianInputValidator()
        {
            RuleFor(input => input.RelationshipCode)
                .NotEmpty()
                .MaximumLength(100);

            // Inline guardian details are only required when no existing guardian is referenced.
            RuleFor(input => input.FirstName)
                .NotEmpty()
                    .WithMessage("Guardian FirstName is required when GuardianId is not supplied.")
                .When(input => !input.GuardianId.HasValue);

            RuleFor(input => input.LastName)
                .NotEmpty()
                    .WithMessage("Guardian LastName is required when GuardianId is not supplied.")
                .When(input => !input.GuardianId.HasValue);

            RuleFor(input => input.FirstName)
                .MaximumLength(100);

            RuleFor(input => input.LastName)
                .MaximumLength(100);

            RuleFor(input => input.Email)
                .EmailAddress()
                .MaximumLength(255)
                .When(input => !string.IsNullOrWhiteSpace(input.Email));

            RuleFor(input => input.Phone)
                .MaximumLength(20);

            RuleFor(input => input.Occupation)
                .MaximumLength(150);

            RuleFor(input => input.Address)
                .MaximumLength(500);
        }
    }
}
