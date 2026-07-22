using Application.Calendars.Commands;
using FluentValidation;

namespace Application.Calendars.Validators
{
    public class UpsertBsMonthLengthsCommandValidator : AbstractValidator<UpsertBsMonthLengthsCommand>
    {
        public UpsertBsMonthLengthsCommandValidator()
        {
            RuleFor(command => command.Items)
                .NotEmpty()
                .WithMessage("At least one month-length item is required.");

            RuleForEach(command => command.Items).ChildRules(item =>
            {
                item.RuleFor(input => input.BsYear)
                    .InclusiveBetween(2000, 2200)
                    .WithMessage("BsYear must be between 2000 and 2200.");

                item.RuleFor(input => input.BsMonth)
                    .InclusiveBetween(1, 12)
                    .WithMessage("BsMonth must be between 1 (Baisakh) and 12 (Chaitra).");

                item.RuleFor(input => input.DaysInMonth)
                    .InclusiveBetween(29, 32)
                    .WithMessage("DaysInMonth must be between 29 and 32 (the official BS month-length range).");
            });
        }
    }
}
