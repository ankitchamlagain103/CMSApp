namespace Domain.Enums
{
    // Shared shape for EmployeeDocument.VerificationStatus and EmployeeQualification.VerificationStatus
    // (2026-08-07) -- same numbering convention as LeaveApprovalStatus. A record starts Pending
    // only when submitted through the self-service "Me" upload/add routes; a record entered
    // through the existing admin route is auto-Approved by the staff member who entered it (see
    // EmployeeService's UploadDocumentInternalAsync/AddQualificationInternalAsync).
    public enum VerificationStatus
    {
        Pending = 1,
        Approved = 2,
        Rejected = 3
    }
}
