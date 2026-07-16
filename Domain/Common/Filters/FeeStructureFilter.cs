using Domain.Enums;

namespace Domain.Common.Filters
{
    public class FeeStructureFilter
    {
        public Guid? AcademicYearId { get; set; }
        public Guid? AcademicClassId { get; set; }
        public RecordStatus? Status { get; set; }
    }
}
