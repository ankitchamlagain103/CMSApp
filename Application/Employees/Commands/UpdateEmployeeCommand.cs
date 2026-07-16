using Domain.Enums;

namespace Application.Employees.Commands
{
    // EmployeeCode is deliberately immutable -- it's the employee's stable business identifier.
    public class UpdateEmployeeCommand
    {
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
        public EmploymentStatus EmploymentStatus { get; set; }
        public string BankName { get; set; }
        public string BankAccountNumber { get; set; }
        public PaymentMode PaymentMode { get; set; }
    }
}
