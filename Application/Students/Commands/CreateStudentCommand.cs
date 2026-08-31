using Domain.Enums;

namespace Application.Students.Commands
{
    public class CreateStudentCommand
    {
        // Optional: leave null/blank and the backend generates the next ADM{year}{seq} number
        // (e.g. ADM2026001); supply a value only for records migrated from an older system.
        //public string AdmissionNo { get; set; }
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

        // Portal account provisioning (2026-07-27), on request -- when true, a login is created
        // for this student in the same call (Email above must be set). Always gets the fixed
        // RoleNames.Student role -- no role picker, unlike Employee's RegisterUserAccount.
        public bool RegisterUserAccount { get; set; }
    }
}
