using Domain.Enums;

namespace Application.Teachers.Dtos
{
    public class TeacherDto
    {
        public Guid Id { get; set; }
        public string EmployeeNo { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public DateTime? JoiningDate { get; set; }
        public RecordStatus Status { get; set; }
    }
}
