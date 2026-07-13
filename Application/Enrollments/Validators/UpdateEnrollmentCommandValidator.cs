using Application.Enrollments.Commands;
using FluentValidation;

namespace Application.Enrollments.Validators
{
    public class UpdateEnrollmentCommandValidator : AbstractValidator<UpdateEnrollmentCommand>
    {
        public UpdateEnrollmentCommandValidator()
        {
            RuleFor(command => command.RollNumber)
                .MaximumLength(20);

            RuleFor(command => command.Status)
                .IsInEnum();
        }
    }
}
