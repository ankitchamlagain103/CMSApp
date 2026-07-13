using Application.AcademicYears.Commands;
using FluentValidation;

namespace Application.AcademicYears.Validators
{
    public class CloneYearStructureCommandValidator : AbstractValidator<CloneYearStructureCommand>
    {
        public CloneYearStructureCommandValidator()
        {
            RuleFor(command => command.SourceAcademicYearId)
                .NotEmpty();
        }
    }
}
