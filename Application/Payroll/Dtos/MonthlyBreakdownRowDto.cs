namespace Application.Payroll.Dtos
{
    // One fiscal month's resolved income/deduction lines and totals. Fiscal-month boundaries are
    // an approximation -- FiscalYear.StartDate..EndDate split into 12 equal-length Gregorian
    // segments and labeled with the canonical Nepali month names in order, since no Bikram Sambat
    // calendar library exists in this codebase (see MonthlyBreakdownCalculator). MonthTax is a
    // flat AnnualTax / 12 for every row, not a cumulative rest-of-year re-projection -- the same
    // "structural, not byte-exact" simplification TaxCalculator itself already documents.
    public class MonthlyBreakdownRowDto
    {
        public int MonthIndex { get; set; }
        public string MonthName { get; set; }
        public DateTime PeriodStartDate { get; set; }
        public DateTime PeriodEndDate { get; set; }
        public int MonthDays { get; set; }
        public List<MonthlyLineItemDto> IncomeLines { get; set; } = new List<MonthlyLineItemDto>();
        public List<MonthlyLineItemDto> DeductionLines { get; set; } = new List<MonthlyLineItemDto>();
        public decimal MonthGrossIncome { get; set; }
        public decimal MonthTax { get; set; }
        public decimal MonthNet { get; set; }
    }
}
