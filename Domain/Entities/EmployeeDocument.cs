using Domain.Enums;

namespace Domain.Entities
{
    // An uploaded verification/identity document for any staff member (citizenship, PAN card,
    // police report, ...) -- 2026-07-23, moved here from a Teacher-only TeacherDocument (renamed
    // table dbo.employee_documents) so every employee, not just teachers, can attach documents.
    // DocumentTypeCode is a Config code (ConfigTypeCodes.DocumentType), validated in the service
    // layer, not a database FK. FilePath is the storage-relative handle returned by
    // IFileStorageService -- never a user-supplied path. ValidUntil is the expiry for documents
    // that have one (driving license, police report). Hard-deleted (the file is removed from
    // storage alongside the row).
    //
    // Verification (2026-08-07): a document submitted through the self-service "Me" route starts
    // VerificationStatus.Pending; one entered through the existing admin route is auto-Approved
    // (see EmployeeService.UploadDocumentInternalAsync). Only a Pending row can be verified/
    // rejected (POST .../documents/{id}/verify|reject, permission-gated -- granted to whichever
    // role a school treats as "HR", not a hardcoded role). VerifiedBy is a username string, same
    // convention as LeaveRequest.ManagerDecisionBy/HrDecisionBy, not a Guid FK.
    public class EmployeeDocument : AuditableEntity
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public string DocumentTypeCode { get; set; }
        public string DocumentName { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string ContentType { get; set; }
        public long FileSizeBytes { get; set; }
        public DateTime? ValidUntil { get; set; }
        public string Remarks { get; set; }
        public VerificationStatus VerificationStatus { get; set; }
        public string VerificationRemarks { get; set; }
        public DateTimeOffset? VerifiedTs { get; set; }
        public string VerifiedBy { get; set; }
        public virtual Employee Employee { get; set; }
    }
}
