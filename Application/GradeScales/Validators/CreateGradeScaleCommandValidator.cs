using Application.GradeScales.Commands;
using FluentValidation;

namespace Application.GradeScales.Validators
{
    public class CreateGradeScaleCommandValidator : AbstractValidator<CreateGradeScaleCommand>
    {
        public CreateGradeScaleCommandValidator()
        {
            RuleFor(command => command.Grade)
                .NotEmpty()
                .MaximumLength(5);

            RuleFor(command => command.MinPercent)
                .GreaterThanOrEqualTo(0)
                .LessThanOrEqualTo(100);

            RuleFor(command => command.MaxPercent)
                .GreaterThanOrEqualTo(0)
                .LessThanOrEqualTo(100);

            RuleFor(command => command.GradePoint)
                .GreaterThanOrEqualTo(0);

            RuleFor(command => command.Remarks)
                .MaximumLength(100);

            RuleFor(command => command)
                .Must(command => command.MaxPercent >= command.MinPercent)
                    .WithMessage("MaxPercent cannot be less than MinPercent.");
        }
    }
}
