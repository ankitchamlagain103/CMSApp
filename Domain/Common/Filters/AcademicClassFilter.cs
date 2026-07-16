using Domain.Enums;

namespace Domain.Common.Filters
{
    // Repository-side filter for GetAcademicClassesQuery -- same rationale as StudentFilter.
    public class AcademicClassFilter
    {
        public Guid? AcademicYearId { get; set; }
        public string GradeCode { get; set; }
        public RecordStatus? Status { get; set; }
    }
}
