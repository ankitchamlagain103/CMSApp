using Domain.Enums;

namespace Application.AcademicClasses.Dtos
{
    // GradeCode/SectionCode are Config codes -- the UI resolves display labels from the dropdown
    // endpoint (GET /api/configs/dropdown/{typeCode}), which it already caches for the selects.
    // Sections are nested so the class list renders one row per class with its sections inside.
    public class AcademicClassDto
    {
        public Guid Id { get; set; }
        public Guid AcademicYearId { get; set; }
        public string GradeCode { get; set; }
        public RecordStatus Status { get; set; }
        public List<ClassSectionDto> Sections { get; set; } = new List<ClassSectionDto>();
    }
}
