namespace Application.Enrollments.Commands
{
    // Students enroll into a section of a class (ClassSectionId), not the class directly.
    public class CreateEnrollmentCommand
    {
        public Guid StudentId { get; set; }
        public Guid ClassSectionId { get; set; }
        public string RollNumber { get; set; }
        public DateTime? EnrollmentDate { get; set; }

        // Onboarding checkboxes (2026-07-17): the OPTIONAL fee items of the class's fee
        // structure this student opts into (transportation, hostel, ...) -- each id must be an
        // IsOptional item on the section's class, validated before anything saves. The UI
        // renders one checkbox per optional item from GET /api/feestructures?academicClassId=.
        // Selections stay editable later via the /fee-selections endpoints.
        public List<Guid> OptionalFeeStructureItemIds { get; set; } = new List<Guid>();
    }
}
