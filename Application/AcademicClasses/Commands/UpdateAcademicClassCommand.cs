using Domain.Enums;

namespace Application.AcademicClasses.Commands
{
    // Year/grade are deliberately immutable -- they ARE the class's identity; changing them under
    // existing enrollments would silently move students. Create a new class instead. Capacity
    // lives on the sections now, not the class.
    public class UpdateAcademicClassCommand
    {
        public RecordStatus Status { get; set; }
    }
}
