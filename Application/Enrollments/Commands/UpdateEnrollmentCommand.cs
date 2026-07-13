using Domain.Enums;

namespace Application.Enrollments.Commands
{
    // Student/class are deliberately immutable -- moving a student is a new enrollment (the old
    // one becomes Transferred/Withdrawn), which preserves the history row.
    public class UpdateEnrollmentCommand
    {
        public string RollNumber { get; set; }
        public DateTime? EnrollmentDate { get; set; }
        public EnrollmentStatus Status { get; set; }
    }
}
