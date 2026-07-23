namespace Application.Employees.Dtos
{
    // "Salary receipt history" / annual forecast grid -- one row per income or retirement-fund
    // line item, one column per fiscal month, matching the reference HRMS's forecast table. A
    // month is "Actual" when a real Approved/Paid SalarySlip already exists for it (real
    // disbursed figures, from that month's snapshotted salary revision); every other month is
    // "Forecast" (projected from the employee's *current* compensation plan, same projection the
    // Tax Details tab already uses). Retirement-fund a/b/c/min lines are only ever populated for
    // Actual months -- see the guide for why this isn't projected forward.
    public class SalaryAnnualForecastDto
    {
        public Guid EmployeeId { get; set; }
        public Guid FiscalYearId { get; set; }
        public string FiscalYearCode { get; set; }
        public List<string> MonthNames { get; set; } = new List<string>();
        public List<bool> IsActualByMonth { get; set; } = new List<bool>();
        public List<SalaryForecastLineDto> IncomeLines { get; set; } = new List<SalaryForecastLineDto>();
        public SalaryForecastLineDto AnnualIncomeForecastLine { get; set; }
        public List<SalaryForecastLineDto> RetirementFundLines { get; set; } = new List<SalaryForecastLineDto>();
    }
}
