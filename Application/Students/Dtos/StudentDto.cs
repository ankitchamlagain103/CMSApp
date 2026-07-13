using Domain.Enums;

namespace Application.Students.Dtos
{
    public class StudentDto
    {
        public Guid Id { get; set; }
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

        // Populated on the detail endpoint (and on create); the paged list leaves it empty to
        // keep the query light.
        public List<StudentGuardianDto> Guardians { get; set; } = new List<StudentGuardianDto>();

        // Detail endpoint only: the active enrollment (current class + subjects studying);
        // null when the student isn't actively enrolled anywhere.
        public StudentCurrentEnrollmentDto CurrentEnrollment { get; set; }
    }
}
