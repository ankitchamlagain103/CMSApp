using Domain.Enums;

namespace Domain.Entities
{
    // Money actually received against an enrollment's invoices. Append-only: corrections go
    // through Status = Voided (reversing this payment's allocations and re-deriving the
    // affected invoices' statuses) plus a fresh payment -- amounts are never edited in place,
    // and receipt numbers are never reused. Soft-deleted on top of that only so an accidental
    // API delete still leaves a trace (financial-audit record, like StudentDiscount).
    public class FeePayment : SoftDeleteAuditableEntity
    {
        public Guid Id { get; set; }
        public string ReceiptNo { get; set; }
        public Guid EnrollmentId { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public PaymentMode PaymentMode { get; set; }
        public string ReferenceNo { get; set; }
        public FeePaymentStatus Status { get; set; }
        public string Remarks { get; set; }

        public virtual Enrollment Enrollment { get; set; }
        public virtual ICollection<FeePaymentAllocation> Allocations { get; set; } = new List<FeePaymentAllocation>();
    }
}
