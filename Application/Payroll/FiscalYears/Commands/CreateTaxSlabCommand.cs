using Domain.Enums;

namespace Application.Payroll.FiscalYears.Commands
{
    public class CreateTaxSlabCommand
    {
        public TaxAssessmentType AssessmentType { get; set; }
        public decimal MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }
        public decimal TaxRate { get; set; }
        public int SlabOrder { get; set; }
    }
}
