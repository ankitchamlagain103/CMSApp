using Application.Exams.Commands;
using FluentValidation;

namespace Application.Exams.Validators
{
    public class StudentExamMarkLineInputValidator : AbstractValidator<StudentExamMarkLineInput>
    {
        public StudentExamMarkLineInputValidator()
        {
            RuleFor(line => line.EnrollmentId)
                .NotEmpty();

            RuleFor(line => line.TheoryObtainedMarks)
                .GreaterThanOrEqualTo(0)
                .When(line => line.TheoryObtainedMarks.HasValue);

            RuleFor(line => line.PracticalObtainedMarks)
                .GreaterThanOrEqualTo(0)
                .When(line => line.PracticalObtainedMarks.HasValue);

            RuleFor(line => line.InternalMarks)
                .GreaterThanOrEqualTo(0)
                .When(line => line.InternalMarks.HasValue);

            RuleFor(line => line.TheoryGraceMarks)
                .GreaterThanOrEqualTo(0);

            RuleFor(line => line.PracticalGraceMarks)
                .GreaterThanOrEqualTo(0);

            RuleFor(line => line.Remarks)
                .MaximumLength(300);
        }
    }
}
