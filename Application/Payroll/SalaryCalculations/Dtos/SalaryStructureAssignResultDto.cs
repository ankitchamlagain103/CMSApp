using Application.Employees.Dtos;

namespace Application.Payroll.SalaryCalculations.Dtos
{
    // Result of assigning a calculated structure to an employee: the calculation that was
    // persisted plus the created salary revision (exactly what POST /api/employees/{id}/salaries
    // would have returned).
    public class SalaryStructureAssignResultDto
    {
        public SalaryStructureCalculationDto Calculation { get; set; }
        public EmployeeSalaryDto Salary { get; set; }
    }
}
