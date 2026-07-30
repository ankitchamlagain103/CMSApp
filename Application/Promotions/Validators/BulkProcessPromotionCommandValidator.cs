using Application.Promotions.Commands;
using FluentValidation;

namespace Application.Promotions.Validators
{
    public class BulkProcessPromotionCommandValidator : AbstractValidator<BulkProcessPromotionCommand>
    {
        public BulkProcessPromotionCommandValidator()
        {
            RuleFor(command => command.ExamTermId)
                .NotEmpty();

            RuleFor(command => command.FromClassSectionId)
                .NotEmpty();

            RuleFor(command => command.PromotedToClassSectionId)
                .NotEmpty();

            RuleFor(command => command.RetainedToClassSectionId)
                .NotEmpty();

            RuleFor(command => command.PromotionDate)
                .NotEmpty();

            RuleFor(command => command.Remarks)
                .MaximumLength(500);
        }
    }
}
