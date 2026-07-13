namespace Application.Teachers.Commands
{
    public class CreateTeacherCommand
    {
        public string EmployeeNo { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public DateTime? JoiningDate { get; set; }
    }
}
