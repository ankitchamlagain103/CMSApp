using Domain.Enums;

namespace Application.AcademicClasses.Commands
{
    // Year/grade are deliberately immutable -- they ARE the class's identity; changing them under
    // existing enrollments would silently move students. Create a new class instead. Capacity
    // lives on the sections now, not the class. Order is pure UI display ordering, not identity,
    // so it's freely editable here.
    public class UpdateAcademicClassCommand
    {
        public int Order { get; set; }
        public RecordStatus Status { get; set; }
    }
}
