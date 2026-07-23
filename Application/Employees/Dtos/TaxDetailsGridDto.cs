namespace Application.Employees.Dtos
{
    // "Tax Details" grid (2026-07-23) -- a flat, spreadsheet-shaped alternative to
    // EmployeeMonthlyTaxBreakdownDto, matching a reference HRMS's Tax Details tab exactly (one
    // row per Particulars line, one column per fiscal month, header/actual-vs-forecast flags
    // instead of a nested months[] structure). See IEmployeeService.GetTaxDetailsGridAsync for how
    // the rows are assembled; a month is Actual (MonthNIsForecast = false) when a real
    // Approved/Paid SalarySlip already exists for it, same rule GetAnnualForecastAsync uses.
    public class TaxDetailsGridDto
    {
        public List<TaxDetailsGridRowDto> List { get; set; } = new List<TaxDetailsGridRowDto>();
        public string Name { get; set; }
        public string Gender { get; set; }
        public string TaxPaidAs { get; set; }

        // Not modeled anywhere in this codebase (no disability/handicapped flag on Employee or
        // any additional-exemption rule in TaxCalculator) -- always false. Kept on the DTO only
        // because the reference UI's Tax Calculation Configuration panel displays it.
        public bool IsHandicapped { get; set; }

        public bool Month1IsForecast { get; set; }
        public bool Month2IsForecast { get; set; }
        public bool Month3IsForecast { get; set; }
        public bool Month4IsForecast { get; set; }
        public bool Month5IsForecast { get; set; }
        public bool Month6IsForecast { get; set; }
        public bool Month7IsForecast { get; set; }
        public bool Month8IsForecast { get; set; }
        public bool Month9IsForecast { get; set; }
        public bool Month10IsForecast { get; set; }
        public bool Month11IsForecast { get; set; }
        public bool Month12IsForecast { get; set; }

        public void SetMonthIsForecast(int monthIndex, bool isForecast)
        {
            switch (monthIndex)
            {
                case 1: Month1IsForecast = isForecast; break;
                case 2: Month2IsForecast = isForecast; break;
                case 3: Month3IsForecast = isForecast; break;
                case 4: Month4IsForecast = isForecast; break;
                case 5: Month5IsForecast = isForecast; break;
                case 6: Month6IsForecast = isForecast; break;
                case 7: Month7IsForecast = isForecast; break;
                case 8: Month8IsForecast = isForecast; break;
                case 9: Month9IsForecast = isForecast; break;
                case 10: Month10IsForecast = isForecast; break;
                case 11: Month11IsForecast = isForecast; break;
                case 12: Month12IsForecast = isForecast; break;
            }
        }
    }
}
