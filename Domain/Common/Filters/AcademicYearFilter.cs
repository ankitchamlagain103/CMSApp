using Domain.Enums;

namespace Domain.Common.Filters
{
    // Repository-side filter for GetAcademicYearsQuery -- same rationale as StudentFilter.
    public class AcademicYearFilter
    {
        public string Search { get; set; }
        public bool? IsCurrent { get; set; }
        public RecordStatus? Status { get; set; }
    }
}
