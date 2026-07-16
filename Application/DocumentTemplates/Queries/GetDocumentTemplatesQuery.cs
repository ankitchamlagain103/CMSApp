using Domain.Enums;

namespace Application.DocumentTemplates.Queries
{
    public class GetDocumentTemplatesQuery
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public DocumentTemplateType? TemplateType { get; set; }
    }
}
