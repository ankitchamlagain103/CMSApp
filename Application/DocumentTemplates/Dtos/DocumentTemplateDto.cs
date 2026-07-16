using Domain.Enums;

namespace Application.DocumentTemplates.Dtos
{
    public class DocumentTemplateDto
    {
        public Guid Id { get; set; }
        public DocumentTemplateType TemplateType { get; set; }
        public string Name { get; set; }
        public string HtmlContent { get; set; }
    }
}
