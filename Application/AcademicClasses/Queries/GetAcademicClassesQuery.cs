using Domain.Enums;

namespace Application.AcademicClasses.Queries
{
    public class GetAcademicClassesQuery
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public Guid? AcademicYearId { get; set; }
        public string GradeCode { get; set; }
        public RecordStatus? Status { get; set; }
    }
}
