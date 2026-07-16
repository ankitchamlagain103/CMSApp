namespace Application.Payroll.Dtos
{
    // One slab's contribution to the total tax -- how much of the income fell in this bracket and
    // how much tax that portion generated.
    public class TaxSlabBreakdownDto
    {
        public decimal MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }
        public decimal TaxRate { get; set; }
        public decimal TaxableAmountInSlab { get; set; }
        public decimal TaxForSlab { get; set; }
    }
}
