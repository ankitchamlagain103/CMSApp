namespace Application.AcademicClasses.Commands
{
    // A class is one grade in one year; its sections come as children so the "New Class" screen
    // can create the class and its sections in a single call. Sections can also be added later
    // via POST /api/academicclasses/{id}/sections.
    public class CreateAcademicClassCommand
    {
        public Guid AcademicYearId { get; set; }
        public string GradeCode { get; set; }
        public List<CreateClassSectionCommand> Sections { get; set; } = new List<CreateClassSectionCommand>();
    }
}
