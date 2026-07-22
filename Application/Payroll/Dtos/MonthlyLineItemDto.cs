namespace Application.Payroll.Dtos
{
    public class MonthlyLineItemDto
    {
        public string Code { get; set; }

        // Human-readable Config catalog label for Code (2026-07-19) -- SalaryComponentType/
        // DeductionType options; falls back to the code itself when no catalog option matches,
        // so it is always displayable.
        public string Label { get; set; }
        public decimal Amount { get; set; }
    }
}
