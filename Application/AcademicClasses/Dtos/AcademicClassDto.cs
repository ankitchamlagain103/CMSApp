using Domain.Enums;

namespace Application.AcademicClasses.Dtos
{
    // GradeCode is a Config code -- GradeLabel resolves it server-side (2026-08-05), so the UI no
    // longer needs its own dropdown-endpoint lookup just to display it. Sections are nested so
    // the class list renders one row per class with its sections inside.
    public class AcademicClassDto
    {
        public Guid Id { get; set; }
        public Guid AcademicYearId { get; set; }
        public string GradeCode { get; set; }
        public string GradeLabel { get; set; }
        public int Order { get; set; }
        public RecordStatus Status { get; set; }
        public List<ClassSectionDto> Sections { get; set; } = new List<ClassSectionDto>();
    }
}
