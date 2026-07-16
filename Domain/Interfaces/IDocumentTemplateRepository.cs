using Domain.Common;
using Domain.Entities;
using Domain.Enums;

namespace Domain.Interfaces
{
    public interface IDocumentTemplateRepository : IRepository<DocumentTemplate, Guid>
    {
        Task<bool> TemplateTypeExistsAsync(DocumentTemplateType templateType, Guid? excludeId = null, CancellationToken cancellationToken = default);

        Task<DocumentTemplate> GetByTemplateTypeAsync(DocumentTemplateType templateType, CancellationToken cancellationToken = default);

        Task<PagedResult<DocumentTemplate>> GetPagedByFilterAsync(DocumentTemplateType? templateType, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    }
}
