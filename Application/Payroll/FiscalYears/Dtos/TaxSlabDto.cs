using Domain.Enums;

namespace Application.Payroll.FiscalYears.Dtos
{
    // MaxAmount null = no upper bound (the top slab). TaxRate is a fraction (0.01 = 1%).
    public class TaxSlabDto
    {
        public Guid Id { get; set; }
        public Guid FiscalYearId { get; set; }
        public TaxAssessmentType AssessmentType { get; set; }
        public decimal MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }
        public decimal TaxRate { get; set; }
        public int SlabOrder { get; set; }
    }
}
