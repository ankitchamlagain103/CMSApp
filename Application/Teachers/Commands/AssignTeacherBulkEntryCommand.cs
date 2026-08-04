namespace Application.Teachers.Commands
{
    // General-purpose bulk entry for one teacher's assignments -- each Items row names its own
    // ClassSubjectId/ClassSectionId/TimePeriodId, so a whole routine grid (several different
    // classes/subjects/sections/periods) can be submitted in one call. Scoped to one teacher (the
    // route id), same as AssignTeacherCommand/AssignTeacherBulkCommand -- there is no
    // logged-in-teacher resolution anywhere in this codebase, so a "many teachers at once" grid
    // isn't in scope here.
    public class AssignTeacherBulkEntryCommand
    {
        public List<TeacherAssignmentEntryItem> Items { get; set; } = new List<TeacherAssignmentEntryItem>();
    }
}
