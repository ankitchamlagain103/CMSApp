using Domain.Enums;

namespace Application.DocumentTemplates.Commands
{
    public class CreateDocumentTemplateCommand
    {
        public DocumentTemplateType TemplateType { get; set; }
        public string Name { get; set; }
        public string HtmlContent { get; set; }
    }
}
