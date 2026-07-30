using Application.Exams.Commands;
using FluentValidation;

namespace Application.Exams.Validators
{
    public class GenerateExamResultsCommandValidator : AbstractValidator<GenerateExamResultsCommand>
    {
        public GenerateExamResultsCommandValidator()
        {
            RuleFor(command => command.ExamTermId)
                .NotEmpty();
        }
    }
}
