using Domain.Enums;

namespace Application.AcademicYears.Queries
{
    // Search matches Code/Name.
    public class GetAcademicYearsQuery
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string Search { get; set; }
        public bool? IsCurrent { get; set; }
        public RecordStatus? Status { get; set; }
    }
}
