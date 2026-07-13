namespace Application.Teachers.Dtos
{
    // What the download endpoint streams back -- the controller turns this into a file result,
    // so it is the one DTO that carries a Stream instead of plain data.
    public class TeacherDocumentFileDto
    {
        public Stream Content { get; set; }
        public string ContentType { get; set; }
        public string FileName { get; set; }
    }
}
