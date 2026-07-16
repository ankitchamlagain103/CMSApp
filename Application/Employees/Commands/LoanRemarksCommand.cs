namespace Application.Employees.Commands
{
    // Shared optional-remarks body for the loan approve/reject/cancel transition endpoints -- all
    // three carry exactly the same one field.
    public class LoanRemarksCommand
    {
        public string Remarks { get; set; }
    }
}
