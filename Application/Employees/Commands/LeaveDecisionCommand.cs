namespace Application.Employees.Commands
{
    // Shared optional-remarks body for the manager-decision/hr-decision approve/reject
    // endpoints -- same "one optional field" shape as LoanRemarksCommand.
    public class LeaveDecisionCommand
    {
        public string Remarks { get; set; }
    }
}
