namespace Application.Enrollments.Dtos
{
    // "How many students got a discount/scholarship" report row -- one per type code.
    public class AwardSummaryDto
    {
        public string TypeCode { get; set; }
        public int StudentCount { get; set; }
    }
}
