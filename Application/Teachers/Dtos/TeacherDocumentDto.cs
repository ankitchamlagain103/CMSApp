namespace Application.Teachers.Dtos
{
    // FilePath is deliberately NOT exposed -- the file is fetched via the download endpoint.
    public class TeacherDocumentDto
    {
        public Guid Id { get; set; }
        public Guid TeacherId { get; set; }
        public string DocumentTypeCode { get; set; }
        public string DocumentName { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public long FileSizeBytes { get; set; }
        public DateTime? ValidUntil { get; set; }
        public string Remarks { get; set; }
        public DateTimeOffset UploadedTs { get; set; }
    }
}
