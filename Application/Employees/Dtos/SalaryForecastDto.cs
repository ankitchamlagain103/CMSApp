using Application.Payroll.Dtos;

namespace Application.Employees.Dtos
{
    // A forward-looking estimate of the next fiscal month's pay, built the same way the old
    // Payslip projection used to be (MonthlyBreakdownCalculator's month row + that month's flat
    // TDS share + any due EmployeeLoan EMI) -- but exposed under its own honest name instead of
    // masquerading as a real payslip. Never backed by a persisted SalarySlip; GetPayslipsAsync/
    // GetPayslipDetailAsync are the source of truth once payroll for a month is actually run and
    // approved (see IEmployeeService.GetSalaryForecastAsync).
    public class SalaryForecastDto
    {
        public Guid EmployeeId { get; set; }
        public Guid SalaryId { get; set; }
        public Guid FiscalYearId { get; set; }
        public string FiscalYearCode { get; set; }
        public int MonthIndex { get; set; }
        public string MonthLabel { get; set; }
        public DateTime PeriodStartDate { get; set; }
        public DateTime PeriodEndDate { get; set; }
        public int MonthDays { get; set; }
        public List<MonthlyLineItemDto> IncomeLines { get; set; } = new List<MonthlyLineItemDto>();
        public decimal GrossSalary { get; set; }
        public List<MonthlyLineItemDto> DeductionLines { get; set; } = new List<MonthlyLineItemDto>();
        public decimal TotalDeductions { get; set; }
        public decimal NetSalary { get; set; }
    }
}
