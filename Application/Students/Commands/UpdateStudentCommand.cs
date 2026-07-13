using Domain.Enums;

namespace Application.Students.Commands
{
    // AdmissionNo is deliberately immutable -- it's the student's stable business identifier.
    public class UpdateStudentCommand
    {
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

        // Three-way semantics, mirroring UpdateUserCommand.RoleIds: null = leave guardian links
        // unchanged; [] = remove every link; non-empty = replace-sync (links absent from the
        // list are removed, entries with GuardianId update/keep the existing link, inline
        // entries create a new guardian + link -- same shapes as the create form).
        public List<StudentGuardianInput> Guardians { get; set; }
    }
}
