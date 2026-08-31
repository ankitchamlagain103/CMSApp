using Application.Exams.Commands;
using FluentValidation;

namespace Application.Exams.Validators
{
    public class CreateStudentExamMarkCommandValidator : AbstractValidator<CreateStudentExamMarkCommand>
    {
        public CreateStudentExamMarkCommandValidator()
        {
            RuleFor(command => command.ExamId)
                .NotEmpty();

            RuleFor(command => command.EnrollmentId)
                .NotEmpty();

            RuleFor(command => command.TheoryObtainedMarks)
                .GreaterThanOrEqualTo(0)
                .When(command => command.TheoryObtainedMarks.HasValue);

            RuleFor(command => command.PracticalObtainedMarks)
                .GreaterThanOrEqualTo(0)
                .When(command => command.PracticalObtainedMarks.HasValue);

            RuleFor(command => command.InternalMarks)
                .GreaterThanOrEqualTo(0)
                .When(command => command.InternalMarks.HasValue);

            RuleFor(command => command.TheoryGraceMarks)
                .GreaterThanOrEqualTo(0);

            RuleFor(command => command.PracticalGraceMarks)
                .GreaterThanOrEqualTo(0);

            RuleFor(command => command.Remarks)
                .MaximumLength(300);
        }
    }
}
