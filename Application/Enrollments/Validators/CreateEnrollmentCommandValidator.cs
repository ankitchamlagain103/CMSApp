using Application.Enrollments.Commands;
using FluentValidation;

namespace Application.Enrollments.Validators
{
    public class CreateEnrollmentCommandValidator : AbstractValidator<CreateEnrollmentCommand>
    {
        public CreateEnrollmentCommandValidator()
        {
            RuleFor(command => command.StudentId)
                .NotEmpty();

            RuleFor(command => command.ClassSectionId)
                .NotEmpty();

            RuleFor(command => command.RollNumber)
                .MaximumLength(20);
        }
    }
}
