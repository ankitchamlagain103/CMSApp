using Domain.Enums;

namespace Domain.Entities
{
    // The header for one class's fee structure -- one row per AcademicClass (since a class is
    // already one grade in one academic year, "the fee changes yearly" falls out for free: next
    // year's class gets its own header rather than overwriting this one). The actual named
    // fee/amount/frequency rows are FeeStructureItem children -- a class's whole fee list is
    // created in one call against this header, not one create per category (2026-07-15 redesign,
    // see Docs/fee_management_implementation_guide.md).
    public class FeeStructure : SoftDeleteAuditableEntity
    {
        public Guid Id { get; set; }
        public Guid AcademicClassId { get; set; }
        public RecordStatus Status { get; set; }
        public virtual AcademicClass AcademicClass { get; set; }
        public virtual ICollection<FeeStructureItem> Items { get; set; } = new List<FeeStructureItem>();
    }
}
