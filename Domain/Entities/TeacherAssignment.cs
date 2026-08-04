namespace Domain.Entities
{
    // Links a teacher to a ClassSubject (i.e. "teaches this subject in this class"), optionally
    // narrowed to one ClassSection (null = teaches it to every section of the class). At most one
    // assignment per (Teacher, ClassSubject, ClassSection); IsClassTeacher requires a section and
    // at most one class teacher exists per ClassSection -- all enforced in the service layer. A
    // teacher can hold several assignments at once across different sections/subjects (e.g. class
    // teacher of one section while also teaching a subject in another) -- nothing here restricts
    // that, the uniqueness rule is purely per (Teacher, ClassSubject, ClassSection) triple.
    // Hard-deleted (pure link row).
    public class TeacherAssignment : AuditableEntity
    {
        public Guid Id { get; set; }
        public Guid TeacherId { get; set; }
        public Guid ClassSubjectId { get; set; }
        public Guid? ClassSectionId { get; set; }
        public bool IsClassTeacher { get; set; }

        // Class period timing (2026-08-03, moved off the Config catalog onto a real FK the same
        // day once it became clear a class-scoped relationship was needed, not a flat option
        // list -- see TimePeriod's doc comment) -- which routine slot this teacher teaches this
        // class/subject in. Optional. A single field, not a day-of-week timetable -- this names
        // one period, not "Mondays and Wednesdays, Period 3". Validated in the service: the
        // picked TimePeriod must be a Period (not Break) AND mapped (via ClassTimePeriod) to the
        // assignment's own class.
        public Guid? TimePeriodId { get; set; }

        public virtual Teacher Teacher { get; set; }
        public virtual ClassSubject ClassSubject { get; set; }
        public virtual ClassSection ClassSection { get; set; }
        public virtual TimePeriod TimePeriod { get; set; }
    }
}
