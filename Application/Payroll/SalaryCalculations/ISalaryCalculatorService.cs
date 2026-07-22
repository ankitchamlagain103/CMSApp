using Application.Common.Models;
using Application.Payroll.SalaryCalculations.Commands;
using Application.Payroll.SalaryCalculations.Dtos;

namespace Application.Payroll.SalaryCalculations
{
    public interface ISalaryCalculatorService
    {
        Task<CommonResponse<SalaryStructureCalculationDto>> CalculateStructureAsync(CalculateSalaryStructureCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<SalaryStructureAssignResultDto>> AssignStructureAsync(AssignSalaryStructureCommand command, CancellationToken cancellationToken = default);
    }
}
