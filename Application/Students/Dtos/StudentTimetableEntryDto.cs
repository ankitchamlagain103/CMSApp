namespace Application.Students.Dtos
{
    // One subject the student studies, with whoever teaches it in their section and (when set)
    // the TimePeriod slot it occupies. Entries are ordered by TimePeriodStartTime (subjects with
    // no period assigned yet sort last, by SubjectCode).
    //
    // Co-teaching simplification: if more than one teacher is assigned to this exact
    // (subject, section) pair, TeacherName lists all of them comma-joined (same convention the
    // old StudentSubjectDto.TeacherName already used), but TeacherId/EmployeeCode/TimePeriod*
    // describe only the first assignment found -- a genuinely rare case, not worth a nested
    // per-teacher structure here.
    public class StudentTimetableEntryDto
    {
        public Guid ClassSubjectId { get; set; }
        public string SubjectCode { get; set; }
        public string SubjectLabel { get; set; }
        public bool IsMandatory { get; set; }
        public Guid? TeacherId { get; set; }
        public string TeacherName { get; set; }
        public string EmployeeCode { get; set; }
        public Guid? TimePeriodId { get; set; }
        public string TimePeriodName { get; set; }
        public TimeSpan? TimePeriodStartTime { get; set; }
        public TimeSpan? TimePeriodEndTime { get; set; }
    }
}
