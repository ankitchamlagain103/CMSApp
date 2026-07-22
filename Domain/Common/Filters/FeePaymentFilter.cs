using Domain.Enums;

namespace Domain.Common.Filters
{
    public class FeePaymentFilter
    {
        public Guid? EnrollmentId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public PaymentMode? PaymentMode { get; set; }
        public FeePaymentStatus? Status { get; set; }
        public string Search { get; set; }
    }
}
