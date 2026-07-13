using Domain.Enums;

namespace Application.Teachers.Commands
{
    // EmployeeNo is deliberately immutable -- it's the teacher's stable business identifier.
    public class UpdateTeacherCommand
    {
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public DateTime? JoiningDate { get; set; }
        public RecordStatus Status { get; set; }
    }
}
