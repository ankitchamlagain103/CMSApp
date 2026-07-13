using Application.AcademicClasses.Commands;
using FluentValidation;

namespace Application.AcademicClasses.Validators
{
    public class UpdateAcademicClassCommandValidator : AbstractValidator<UpdateAcademicClassCommand>
    {
        public UpdateAcademicClassCommandValidator()
        {
            RuleFor(command => command.Status)
                .IsInEnum();
        }
    }
}
