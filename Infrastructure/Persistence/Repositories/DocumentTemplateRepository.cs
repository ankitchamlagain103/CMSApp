using Domain.Common;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories
{
    public class DocumentTemplateRepository : Repository<DocumentTemplate, Guid>, IDocumentTemplateRepository
    {
        public DocumentTemplateRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<bool> TemplateTypeExistsAsync(DocumentTemplateType templateType, Guid? excludeId = null, CancellationToken cancellationToken = default)
        {
            IQueryable<DocumentTemplate> templatesQuery = DbSet.Where(t => t.TemplateType == templateType);

            if (excludeId.HasValue)
            {
                templatesQuery = templatesQuery.Where(t => t.Id != excludeId.Value);
            }

            var exists = await templatesQuery.AnyAsync(cancellationToken);

            return exists;
        }

        public async Task<DocumentTemplate> GetByTemplateTypeAsync(DocumentTemplateType templateType, CancellationToken cancellationToken = default)
        {
            var documentTemplate = await DbSet.FirstOrDefaultAsync(t => t.TemplateType == templateType, cancellationToken);

            return documentTemplate;
        }

        public async Task<PagedResult<DocumentTemplate>> GetPagedByFilterAsync(DocumentTemplateType? templateType, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            IQueryable<DocumentTemplate> templatesQuery = DbSet;

            if (templateType.HasValue)
            {
                templatesQuery = templatesQuery.Where(t => t.TemplateType == templateType.Value);
            }

            var totalCount = await templatesQuery.CountAsync(cancellationToken);
            var skipCount = (pageNumber - 1) * pageSize;
            var items = await templatesQuery
                .OrderBy(t => t.TemplateType)
                .Skip(skipCount)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var pagedResult = new PagedResult<DocumentTemplate>
            {
                Items = items,
                TotalCount = totalCount
            };

            return pagedResult;
        }
    }
}
