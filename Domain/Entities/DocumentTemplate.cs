using Domain.Enums;

namespace Domain.Entities
{
    public class DocumentTemplate : AuditableEntity
    {
        public Guid Id { get; set; }
        public DocumentTemplateType TemplateType { get; set; }
        public string Name { get; set; }
        public string HtmlContent { get; set; }
    }
}
