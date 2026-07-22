using Domain.Enums;

namespace Application.FeeInvoices.Queries
{
    public class GetFeeAdjustmentsQuery
    {
        public Guid? EnrollmentId { get; set; }
        public int? BillingYear { get; set; }
        public int? BillingMonth { get; set; }
        public AdjustmentStatus? Status { get; set; }
    }
}
