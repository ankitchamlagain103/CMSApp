namespace Application.Employees.Commands
{
    // Fixes the gap where an already-created TeacherAssignment (see AssignTeacherCommand) had no
    // way to change its TimePeriodId afterward: the bulk-entry/single-assign endpoints only ever
    // create, and creating again over an existing (teacher, classSubject, section) triple 409s as
    // a duplicate instead of updating it -- so an admin picking a period for an assignment that
    // already exists silently did nothing. This lets that one field be edited in place.
    // Null clears the period back to "not set", same as omitting it on create.
    public class UpdateAssignmentTimePeriodCommand
    {
        public Guid? TimePeriodId { get; set; }
    }
}
