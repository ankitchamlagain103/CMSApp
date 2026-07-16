namespace Application.Students.Dtos
{
    // What the download endpoint streams back -- the controller turns this into a file result.
    public class StudentDocumentFileDto
    {
        public Stream Content { get; set; }
        public string ContentType { get; set; }
        public string FileName { get; set; }
    }
}
