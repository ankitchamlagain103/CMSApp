using Domain.Enums;

namespace Domain.Entities
{
    // One progressive income-tax bracket for a FiscalYear. MaxAmount null = no upper bound (the
    // top slab). AssessmentType lets Individual/Couple carry different thresholds, per Nepal's
    // income tax rules. Pure config child row, hard-deleted (same shape as ClassSubject).
    public class TaxSlab : AuditableEntity
    {
        public Guid Id { get; set; }
        public Guid FiscalYearId { get; set; }
        public TaxAssessmentType AssessmentType { get; set; }
        public decimal MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }
        public decimal TaxRate { get; set; }
        public int SlabOrder { get; set; }
        public virtual FiscalYear FiscalYear { get; set; }
    }
}
