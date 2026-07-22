namespace Domain.Common.Filters
{
    public class FeeGenerationRunFilter
    {
        public Guid? AcademicYearId { get; set; }
        public int? BillingYear { get; set; }
        public int? BillingMonth { get; set; }
    }
}
