using Domain.Enums;

namespace Domain.Entities
{
    // One billing month for one enrollment -- the persisted, auditable snapshot the fee
    // redesign introduces on top of the FeeStructure configuration layer. Totals are stored
    // (recomputed on every Draft edit and once more at finalization), not derived on read,
    // because a finalized invoice is an immutable financial record: later fee-structure or
    // discount edits must never change what January's invoice said.
    //
    // PreviousDueAmount is an informational snapshot of the enrollment's outstanding balance
    // across earlier invoices at generation time (for the printed receipt's "previous dues"
    // row) -- the authoritative outstanding is always computed live across open invoices.
    // AcademicYearId is denormalized from the enrollment's section->class->year chain for
    // cheap year filtering.
    public class FeeInvoice : SoftDeleteAuditableEntity
    {
        public Guid Id { get; set; }
        public string InvoiceNo { get; set; }
        public Guid EnrollmentId { get; set; }
        public Guid AcademicYearId { get; set; }
        public int BillingYear { get; set; }
        public int BillingMonth { get; set; }
        public FeeInvoiceStatus Status { get; set; }
        public decimal GrossAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal NetAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal PreviousDueAmount { get; set; }

        // > 0 once this invoice's remaining balance has been carried forward and the invoice
        // voided (Status flips to Cancelled at the same time -- see CarriedForwardToInvoiceId).
        // Kept purely for display ("Rs 5,250 was carried forward from this invoice"); the
        // Cancelled status alone is what excludes it from outstanding-balance sums (statement,
        // student search, payment allocation) and the account-statement ledger.
        public decimal CarriedForwardAmount { get; set; }

        // Which later invoice absorbed this invoice's balance as a CARRY_CORRECTION line -- the
        // reference a voided invoice carries forward. Plain scalar, no FK/navigation (same
        // lineage-column convention as FeeInvoiceLine.FeeStructureItemId etc.): the two invoices
        // aren't a real relational parent/child, just a pointer for traceability.
        public Guid? CarriedForwardToInvoiceId { get; set; }

        public DateTime DueDate { get; set; }
        public DateTime? GeneratedTs { get; set; }
        public string Remarks { get; set; }

        public virtual Enrollment Enrollment { get; set; }
        public virtual AcademicYear AcademicYear { get; set; }
        public virtual ICollection<FeeInvoiceLine> Lines { get; set; } = new List<FeeInvoiceLine>();
        public virtual ICollection<FeePaymentAllocation> Allocations { get; set; } = new List<FeePaymentAllocation>();
    }
}
