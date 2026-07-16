namespace Domain.Common
{
    // One row of a "how many students got X" report -- shared shape for the discount and
    // scholarship summaries (GetDiscountSummaryAsync/GetScholarshipSummaryAsync on
    // IEnrollmentRepository).
    public class AwardSummaryItem
    {
        public string TypeCode { get; set; }
        public int StudentCount { get; set; }
    }
}
