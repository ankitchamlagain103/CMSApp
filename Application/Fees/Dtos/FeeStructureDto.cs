using Domain.Enums;

namespace Application.Fees.Dtos
{
    // GradeCode/AcademicYearId are flattened in off the class so the fee list doesn't need a
    // second call per row to know what it's pricing. Items is the class's full named fee list
    // (2026-07-15 header+items redesign -- see Docs/fee_management_implementation_guide.md).
    public class FeeStructureDto
    {
        public Guid Id { get; set; }
        public Guid AcademicClassId { get; set; }
        public Guid AcademicYearId { get; set; }
        public string GradeCode { get; set; }
        public RecordStatus Status { get; set; }
        public List<FeeStructureItemDto> Items { get; set; } = new List<FeeStructureItemDto>();
    }
}
