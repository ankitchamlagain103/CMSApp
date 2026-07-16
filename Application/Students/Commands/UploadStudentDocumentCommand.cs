namespace Application.Students.Commands
{
    // Metadata half of a document upload (the file itself arrives as multipart form data and is
    // passed to the service as a stream). ValidUntil applies where a document expires; most
    // student documents (birth certificate, marksheet) leave it null.
    public class UploadStudentDocumentCommand
    {
        public string DocumentTypeCode { get; set; }
        public string DocumentName { get; set; }
        public DateTime? ValidUntil { get; set; }
        public string Remarks { get; set; }
    }
}
