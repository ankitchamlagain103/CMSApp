using Domain.Enums;

namespace Application.FeePayments.Dtos
{
    public class FeePaymentDto
    {
        public Guid Id { get; set; }
        public string ReceiptNo { get; set; }
        public Guid EnrollmentId { get; set; }
        public string StudentName { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public PaymentMode PaymentMode { get; set; }
        public string ReferenceNo { get; set; }
        public FeePaymentStatus Status { get; set; }
        public string Remarks { get; set; }

        // How many not-yet-existing months this payment billed in advance (2026-07-17) --
        // Allocations already lists each one, IsNewlyGenerated marks which.
        public int MonthsBilledInAdvance { get; set; }
        public List<FeePaymentAllocationDto> Allocations { get; set; } = new List<FeePaymentAllocationDto>();
    }
}
