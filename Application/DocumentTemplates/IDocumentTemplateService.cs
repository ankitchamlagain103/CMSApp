using Application.Common.Models;
using Application.DocumentTemplates.Commands;
using Application.DocumentTemplates.Dtos;
using Application.DocumentTemplates.Queries;
using Domain.Enums;

namespace Application.DocumentTemplates
{
    public interface IDocumentTemplateService
    {
        Task<CommonResponse<DocumentTemplateDto>> CreateDocumentTemplateAsync(CreateDocumentTemplateCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<DocumentTemplateDto>> GetDocumentTemplateByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<CommonResponse<PaginatedResponse<DocumentTemplateDto>>> GetDocumentTemplatesAsync(GetDocumentTemplatesQuery query, CancellationToken cancellationToken = default);

        Task<CommonResponse<DocumentTemplateDto>> UpdateDocumentTemplateAsync(Guid id, UpdateDocumentTemplateCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> DeleteDocumentTemplateAsync(Guid id, CancellationToken cancellationToken = default);

        Task<CommonResponse<List<TemplatePlaceholderDto>>> GetPlaceholdersAsync(DocumentTemplateType templateType, CancellationToken cancellationToken = default);
    }
}
