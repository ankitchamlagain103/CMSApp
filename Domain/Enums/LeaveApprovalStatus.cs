namespace Domain.Enums
{
    // Shared shape for LeaveRequest.ManagerStatus and LeaveRequest.HrStatus -- each field tracks
    // its own reviewer's decision independently (see LeaveRequest's own doc comment for how the
    // two combine into an effective/overall status).
    public enum LeaveApprovalStatus
    {
        Pending = 1,
        Approved = 2,
        Rejected = 3
    }
}
