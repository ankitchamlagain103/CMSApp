namespace Domain.Entities
{
    // An uploaded admission/record document for a student (birth certificate, transfer
    // certificate, marksheet, ...). Mirrors EmployeeDocument: DocumentTypeCode is a Config code
    // (ConfigTypeCodes.StudentDocumentType), FilePath is the IFileStorageService handle (never a
    // user-supplied path), ValidUntil is the expiry where one applies. Hard-deleted (the file is
    // removed from storage alongside the row).
    public class StudentDocument : AuditableEntity
    {
        public Guid Id { get; set; }
        public Guid StudentId { get; set; }
        public string DocumentTypeCode { get; set; }
        public string DocumentName { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string ContentType { get; set; }
        public long FileSizeBytes { get; set; }
        public DateTime? ValidUntil { get; set; }
        public string Remarks { get; set; }
        public virtual Student Student { get; set; }
    }
}
