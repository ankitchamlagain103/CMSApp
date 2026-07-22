using Application.Payroll.SalaryCalculations.Commands;
using FluentValidation;

namespace Application.Payroll.SalaryCalculations.Validators
{
    public class AssignSalaryStructureCommandValidator : AbstractValidator<AssignSalaryStructureCommand>
    {
        public AssignSalaryStructureCommandValidator()
        {
            Include(new CalculateSalaryStructureCommandValidator());

            RuleFor(x => x.EmployeeId)
                .NotEmpty()
                .WithMessage("EmployeeId is required.");

            RuleFor(x => x.EffectiveFromDate)
                .NotEmpty()
                .WithMessage("EffectiveFromDate is required.");
        }
    }
}
