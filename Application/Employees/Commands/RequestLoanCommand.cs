namespace Application.Employees.Commands
{
    public class RequestLoanCommand
    {
        public string LoanTypeCode { get; set; }
        public decimal PrincipalAmount { get; set; }
        public decimal EmiAmount { get; set; }
        public DateTime StartDate { get; set; }
        public string Remarks { get; set; }
    }
}
