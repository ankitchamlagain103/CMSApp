namespace Application.Employees.Commands
{
    // Shared optional-remarks body for the qualification verify/reject endpoints -- same "one
    // optional field" shape as LeaveDecisionCommand/LoanRemarksCommand.
    public class QualificationVerificationCommand
    {
        public string Remarks { get; set; }
    }
}
