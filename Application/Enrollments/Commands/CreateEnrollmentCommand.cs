namespace Application.Enrollments.Commands
{
    // Students enroll into a section of a class (ClassSectionId), not the class directly.
    public class CreateEnrollmentCommand
    {
        public Guid StudentId { get; set; }
        public Guid ClassSectionId { get; set; }
        public string RollNumber { get; set; }
        public DateTime? EnrollmentDate { get; set; }
    }
}
