using Domain.Enums;

namespace Application.Students.Dtos
{
    public class StudentDto
    {
        public Guid Id { get; set; }

        // Non-null only once a portal account has been provisioned for this student (2026-07-27).
        public Guid? UserId { get; set; }

        public string AdmissionNo { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public Gender Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public DateTime? AdmissionDate { get; set; }
        public RecordStatus Status { get; set; }
        public string CreatedBy { get; set; }
        public DateTimeOffset CreatedTs { get; set; }
        public string UpdatedBy { get; set; }
        public DateTimeOffset? UpdatedTs { get; set; }

        // Populated on create/update only (2026-08-05) -- those responses are the "what did I just
        // submit" confirmation, so returning it there saves an immediate re-fetch. GetStudentByIdAsync
        // leaves it empty, same as the paged list always has -- the "Guardians" tab has its own
        // GET /api/students/{id}/guardians, so embedding the full list in every profile GET was
        // duplicate data nobody asked for on every page load.
        public List<StudentGuardianDto> Guardians { get; set; } = new List<StudentGuardianDto>();

        // Lightweight "current class" indicator (EnrollmentId/grade/section/roll/studying-since) --
        // null when the student isn't actively enrolled anywhere. Deliberately does NOT carry the
        // subject list (see StudentCurrentEnrollmentDto's own doc comment) -- that's
        // GET /api/students/{id}/timetable's job, loaded only when the "Current Class" tab opens.
        public StudentCurrentEnrollmentDto CurrentEnrollment { get; set; }
    }
}
