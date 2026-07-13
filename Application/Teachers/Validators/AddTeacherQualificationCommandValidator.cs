using Application.Teachers.Commands;
using FluentValidation;

namespace Application.Teachers.Validators
{
    public class AddTeacherQualificationCommandValidator : AbstractValidator<AddTeacherQualificationCommand>
    {
        public AddTeacherQualificationCommandValidator()
        {
            RuleFor(command => command.QualificationCode)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(command => command.CourseName)
                .MaximumLength(200);

            RuleFor(command => command.Institution)
                .MaximumLength(255);

            RuleFor(command => command.CompletionYear)
                .InclusiveBetween(1950, 2100)
                .When(command => command.CompletionYear.HasValue);

            RuleFor(command => command.Score)
                .MaximumLength(50);

            RuleFor(command => command.Remarks)
                .MaximumLength(500);
        }
    }
}
