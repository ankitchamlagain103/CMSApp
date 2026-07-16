using Domain.Enums;

namespace Application.Fees.Queries
{
    public class GetFeeStructuresQuery
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public Guid? AcademicYearId { get; set; }
        public Guid? AcademicClassId { get; set; }
        public RecordStatus? Status { get; set; }
    }
}
