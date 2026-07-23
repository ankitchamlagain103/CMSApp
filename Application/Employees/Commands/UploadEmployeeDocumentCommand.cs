namespace Application.Employees.Commands
{
    // Metadata half of a document upload (the file itself arrives as multipart form data and is
    // passed to the service as a stream). ValidUntil is the expiry date for documents that have
    // one (driving license, police report); leave null for evergreen documents.
    public class UploadEmployeeDocumentCommand
    {
        public string DocumentTypeCode { get; set; }
        public string DocumentName { get; set; }
        public DateTime? ValidUntil { get; set; }
        public string Remarks { get; set; }
    }
}
