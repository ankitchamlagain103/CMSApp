using Application.DocumentTemplates.Dtos;
using Domain.Entities;

namespace Application.DocumentTemplates
{
    public static class DocumentTemplateMapper
    {
        public static DocumentTemplateDto ToDto(DocumentTemplate documentTemplate)
        {
            var documentTemplateDto = new DocumentTemplateDto
            {
                Id = documentTemplate.Id,
                TemplateType = documentTemplate.TemplateType,
                Name = documentTemplate.Name,
                HtmlContent = documentTemplate.HtmlContent
            };

            return documentTemplateDto;
        }
    }
}
