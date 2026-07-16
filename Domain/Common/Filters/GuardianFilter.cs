namespace Domain.Common.Filters
{
    // Repository-side filter for GetGuardiansQuery -- same rationale as StudentFilter.
    public class GuardianFilter
    {
        public string Search { get; set; }
        public string Phone { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
