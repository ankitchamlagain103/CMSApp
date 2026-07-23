namespace Application.Employees.Dtos
{
    // One row of the Tax Details grid (2026-07-23) -- flat spreadsheet shape matching the
    // reference HRMS's Tax Details table (Particulars | Description | Annual Amount | one column
    // per fiscal month). IsBold/IsTab exist for the consuming grid's styling hooks but are always
    // false from this backend today -- nothing here currently needs a bold or indented row; the
    // fields are kept so the frontend grid component's contract doesn't need a follow-up change if
    // that's ever introduced. See TaxDetailsGridDto for the parent shape.
    public class TaxDetailsGridRowDto
    {
        public int RowNumber { get; set; }
        public bool IsHeader { get; set; }
        public bool IsBold { get; set; }
        public bool IsTab { get; set; }
        public string Particulars { get; set; }
        public string Description { get; set; }
        public decimal Total { get; set; }
        public decimal Month1Amount { get; set; }
        public decimal Month2Amount { get; set; }
        public decimal Month3Amount { get; set; }
        public decimal Month4Amount { get; set; }
        public decimal Month5Amount { get; set; }
        public decimal Month6Amount { get; set; }
        public decimal Month7Amount { get; set; }
        public decimal Month8Amount { get; set; }
        public decimal Month9Amount { get; set; }
        public decimal Month10Amount { get; set; }
        public decimal Month11Amount { get; set; }
        public decimal Month12Amount { get; set; }

        // 1-based month-index setter/getter so the builder can loop 1..12 instead of writing out
        // a 12-case switch by hand at every call site.
        public void SetMonthAmount(int monthIndex, decimal amount)
        {
            switch (monthIndex)
            {
                case 1: Month1Amount = amount; break;
                case 2: Month2Amount = amount; break;
                case 3: Month3Amount = amount; break;
                case 4: Month4Amount = amount; break;
                case 5: Month5Amount = amount; break;
                case 6: Month6Amount = amount; break;
                case 7: Month7Amount = amount; break;
                case 8: Month8Amount = amount; break;
                case 9: Month9Amount = amount; break;
                case 10: Month10Amount = amount; break;
                case 11: Month11Amount = amount; break;
                case 12: Month12Amount = amount; break;
            }
        }

        public decimal GetMonthAmount(int monthIndex)
        {
            switch (monthIndex)
            {
                case 1: return Month1Amount;
                case 2: return Month2Amount;
                case 3: return Month3Amount;
                case 4: return Month4Amount;
                case 5: return Month5Amount;
                case 6: return Month6Amount;
                case 7: return Month7Amount;
                case 8: return Month8Amount;
                case 9: return Month9Amount;
                case 10: return Month10Amount;
                case 11: return Month11Amount;
                default: return Month12Amount;
            }
        }
    }
}
