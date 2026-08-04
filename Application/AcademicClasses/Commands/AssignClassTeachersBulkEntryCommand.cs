namespace Application.AcademicClasses.Commands
{
    // Class-scoped counterpart to Application.Teachers.Commands.AssignTeacherBulkEntryCommand --
    // that one is scoped to one teacher (route id) and lets each row name its own class/subject/
    // section/period; this one is scoped to one AcademicClass (route id) and lets each row name
    // its own teacher, so an admin working from a class's page can map every teacher who teaches
    // that class -- across its subjects, sections, and time periods -- in a single submission.
    public class AssignClassTeachersBulkEntryCommand
    {
        public List<ClassTeacherAssignmentEntryItem> Items { get; set; } = new List<ClassTeacherAssignmentEntryItem>();
    }
}
