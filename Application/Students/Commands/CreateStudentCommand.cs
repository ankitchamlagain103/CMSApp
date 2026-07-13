using Domain.Enums;

namespace Application.Students.Commands
{
    public class CreateStudentCommand
    {
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

        // Guardians captured during onboarding -- optional, but the typical admission flow sends
        // at least one (existing guardian by id, or inline details to create one). Guardians can
        // still be linked later via POST /api/students/{id}/guardians.
        public List<StudentGuardianInput> Guardians { get; set; } = new List<StudentGuardianInput>();
    }
}
