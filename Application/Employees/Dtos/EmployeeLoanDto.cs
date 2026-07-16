using Domain.Enums;

namespace Application.Employees.Dtos
{
    // AmountRepaid/RemainingBalance/IsFullyRepaid are computed as of "now" (LoanCalculator), not
    // stored -- see EmployeeLoan's own header comment for why there's no separate repayment
    // ledger table.
    public class EmployeeLoanDto
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public string LoanTypeCode { get; set; }
        public decimal PrincipalAmount { get; set; }
        public decimal EmiAmount { get; set; }
        public DateTime RequestedDate { get; set; }
        public DateTime StartDate { get; set; }
        public LoanStatus Status { get; set; }
        public string Remarks { get; set; }
        public decimal AmountRepaid { get; set; }
        public decimal RemainingBalance { get; set; }
        public bool IsFullyRepaid { get; set; }
    }
}
