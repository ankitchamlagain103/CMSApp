using Application.Payroll.Dtos;

namespace Application.Employees.Dtos
{
    // Wraps TaxCalculator's annual result (same TaxCalculationResultDto the Investment & Tax
    // Planning tab already renders) alongside MonthlyBreakdownCalculator's 12 fiscal-month rows,
    // so the Tax Details tab can show both without re-fetching or risking the two disagreeing.
    public class EmployeeMonthlyTaxBreakdownDto
    {
        public Guid EmployeeId { get; set; }
        public Guid SalaryId { get; set; }
        public Guid FiscalYearId { get; set; }
        public string FiscalYearCode { get; set; }
        public TaxCalculationResultDto TaxCalculation { get; set; }
        public List<MonthlyBreakdownRowDto> Months { get; set; } = new List<MonthlyBreakdownRowDto>();
    }
}
