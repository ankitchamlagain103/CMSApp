using Application.Promotions.Commands;
using Domain.Enums;
using FluentValidation;

namespace Application.Promotions.Validators
{
    public class CreateStudentPromotionCommandValidator : AbstractValidator<CreateStudentPromotionCommand>
    {
        public CreateStudentPromotionCommandValidator()
        {
            RuleFor(command => command.FromEnrollmentId)
                .NotEmpty();

            RuleFor(command => command.ToClassSectionId)
                .NotEmpty();

            RuleFor(command => command.PromotionDate)
                .NotEmpty();

            RuleFor(command => command.PromotionType)
                .IsInEnum();

            RuleFor(command => command.Remarks)
                .MaximumLength(500);
        }
    }
}
