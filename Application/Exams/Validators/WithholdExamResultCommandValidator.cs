using Application.Exams.Commands;
using FluentValidation;

namespace Application.Exams.Validators
{
    public class WithholdExamResultCommandValidator : AbstractValidator<WithholdExamResultCommand>
    {
        public WithholdExamResultCommandValidator()
        {
            RuleFor(command => command.Remarks)
                .NotEmpty()
                .MaximumLength(500);
        }
    }
}
