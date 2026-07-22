namespace Application.FeeGenerationRuns.Queries
{
    public class GetFeeGenerationRunsQuery
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public Guid? AcademicYearId { get; set; }
        public int? BillingYear { get; set; }
        public int? BillingMonth { get; set; }
    }
}
