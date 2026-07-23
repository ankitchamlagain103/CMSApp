namespace Application.Employees.Dtos
{
    // One row of the Annual Salary Forecast / history grid -- a single income or retirement-fund
    // line item, with one amount per fiscal month alongside the annual total. A null entry in
    // MonthlyAmounts means "not applicable/not computed for this month" (the retirement-fund a/b/
    // c/min rows are only ever populated for months that already have a real, Approved/Paid
    // payslip -- see SalaryAnnualForecastDto.IsActualByMonth).
    public class SalaryForecastLineDto
    {
        public string Code { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }
        public decimal AnnualAmount { get; set; }
        public List<decimal?> MonthlyAmounts { get; set; } = new List<decimal?>();
    }
}
