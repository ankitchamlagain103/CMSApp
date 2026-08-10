namespace Application.Employees.Commands
{
    // Shared optional-remarks body for the document verify/reject endpoints -- same "one optional
    // field" shape as LeaveDecisionCommand/LoanRemarksCommand.
    public class DocumentVerificationCommand
    {
        public string Remarks { get; set; }
    }
}
