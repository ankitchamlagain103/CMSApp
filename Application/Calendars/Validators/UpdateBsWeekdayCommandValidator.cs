using Application.Calendars.Commands;
using FluentValidation;

namespace Application.Calendars.Validators
{
    public class UpdateBsWeekdayCommandValidator : AbstractValidator<UpdateBsWeekdayCommand>
    {
        public UpdateBsWeekdayCommandValidator()
        {
            RuleFor(command => command.NameEn)
                .NotEmpty()
                .MaximumLength(50);

            RuleFor(command => command.NameNp)
                .NotEmpty()
                .MaximumLength(50);
        }
    }
}
