using Domain.Enums;

namespace Application.AcademicClasses.Dtos
{
    public class ClassSectionDto
    {
        public Guid Id { get; set; }
        public Guid AcademicClassId { get; set; }
        public string SectionCode { get; set; }
        public int Capacity { get; set; }
        public RecordStatus Status { get; set; }
    }
}
