namespace Application.Payroll.FiscalYears.Commands
{
    // AssessmentType is immutable -- it's part of the slab's identity within the fiscal year;
    // remove and re-add instead of moving a slab between Individual/Couple.
    public class UpdateTaxSlabCommand
    {
        public decimal MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }
        public decimal TaxRate { get; set; }
        public int SlabOrder { get; set; }
    }
}
