using Application.Teachers.Commands;
using FluentValidation;

namespace Application.Teachers.Validators
{
    public class CreateTeacherCommandValidator : AbstractValidator<CreateTeacherCommand>
    {
        public CreateTeacherCommandValidator()
        {
            RuleFor(command => command.EmployeeNo)
                .NotEmpty()
                .MaximumLength(30);

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
                .When(command => !string.IsNullOrEmpty(command.Email));

            RuleFor(command => command.Phone)
                .MaximumLength(20);
        }
    }
}
