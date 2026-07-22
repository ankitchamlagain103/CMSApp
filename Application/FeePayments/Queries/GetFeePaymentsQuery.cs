using Domain.Enums;

namespace Application.FeePayments.Queries
{
    public class GetFeePaymentsQuery
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public Guid? EnrollmentId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public PaymentMode? PaymentMode { get; set; }
        public FeePaymentStatus? Status { get; set; }

        // Matches the student's first/last name, admission no, or email (case-insensitive,
        // substring), or the receipt no.
        public string Search { get; set; }
    }
}
