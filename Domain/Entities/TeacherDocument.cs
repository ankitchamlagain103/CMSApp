namespace Domain.Entities
{
    // An uploaded verification/identity document for a teacher (citizenship, PAN card, police
    // report, ...). DocumentTypeCode is a Config code (ConfigTypeCodes.DocumentType), validated
    // in the service layer, not a database FK. FilePath is the storage-relative handle returned
    // by IFileStorageService -- never a user-supplied path. ValidUntil is the expiry for
    // documents that have one (driving license, police report). Hard-deleted (the file is
    // removed from storage alongside the row).
    public class TeacherDocument : AuditableEntity
    {
        public Guid Id { get; set; }
        public Guid TeacherId { get; set; }
        public string DocumentTypeCode { get; set; }
        public string DocumentName { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string ContentType { get; set; }
        public long FileSizeBytes { get; set; }
        public DateTime? ValidUntil { get; set; }
        public string Remarks { get; set; }
        public virtual Teacher Teacher { get; set; }
    }
}
