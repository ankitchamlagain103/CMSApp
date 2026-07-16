using Application.Payroll.Dtos;

namespace Application.Employees.Dtos
{
    // Wraps TaxCalculator's result with the context it was computed for -- which salary revision
    // and which fiscal year's slabs were used.
    public class EmployeeTaxCalculationDto
    {
        public Guid EmployeeId { get; set; }
        public Guid SalaryId { get; set; }
        public Guid FiscalYearId { get; set; }
        public string FiscalYearCode { get; set; }
        public decimal GrossMonthly { get; set; }
        public TaxCalculationResultDto TaxCalculation { get; set; }
        public decimal NetMonthly { get; set; }
    }
}
