using Domain.Enums;

namespace Application.AcademicClasses.Commands
{
    // SectionCode is deliberately immutable -- it IS the section's identity; renaming it under
    // existing enrollments would silently move students. Create a new section instead.
    public class UpdateClassSectionCommand
    {
        public int Capacity { get; set; }
        public RecordStatus Status { get; set; }
    }
}
