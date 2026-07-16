using Domain.Enums;

namespace Application.Employees.Commands
{
    // EmployeeCode optional -- blank = auto-generated EMP{year}{seq} (shared sequence across every
    // employee type, same helper Teacher/Student creation already use).
    public class CreateEmployeeCommand
    {
        public string EmployeeCode { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public Gender Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public DateTime? JoinDate { get; set; }
        public string EmployeeCategoryCode { get; set; }
        public string JobPositionCode { get; set; }
        public string BankName { get; set; }
        public string BankAccountNumber { get; set; }
        public PaymentMode PaymentMode { get; set; }
    }
}
