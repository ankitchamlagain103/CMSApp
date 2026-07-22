using Domain.Common;
using Domain.Common.Filters;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories
{
    public class FeeRuleRepository : Repository<FeeRule, Guid>, IFeeRuleRepository
    {
        public FeeRuleRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<PagedResult<FeeRule>> GetPagedByFilterAsync(FeeRuleFilter filter, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            IQueryable<FeeRule> rulesQuery = DbSet.Include(r => r.AcademicClass);

            if (filter.RuleType.HasValue)
            {
                rulesQuery = rulesQuery.Where(r => r.RuleType == filter.RuleType.Value);
            }

            if (filter.AcademicClassId.HasValue)
            {
                rulesQuery = rulesQuery.Where(r => r.AcademicClassId == filter.AcademicClassId.Value);
            }

            if (filter.IsActive.HasValue)
            {
                rulesQuery = rulesQuery.Where(r => r.IsActive == filter.IsActive.Value);
            }

            var totalCount = await rulesQuery.CountAsync(cancellationToken);
            var skipCount = (pageNumber - 1) * pageSize;
            var items = await rulesQuery
                .OrderBy(r => r.Priority)
                .ThenBy(r => r.Code)
                .Skip(skipCount)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var pagedResult = new PagedResult<FeeRule>
            {
                Items = items,
                TotalCount = totalCount
            };

            return pagedResult;
        }

        public async Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default)
        {
            // IgnoreQueryFilters: a soft-deleted rule still owns its Code (unique index).
            var exists = await DbSet
                .IgnoreQueryFilters()
                .AnyAsync(r => r.Code == code, cancellationToken);

            return exists;
        }

        public async Task<IReadOnlyList<FeeRule>> GetActiveRulesAsync(FeeRuleTrigger triggerStage, DateTime asOfDate, CancellationToken cancellationToken = default)
        {
            var rules = await DbSet
                .Where(r => r.IsActive
                    && r.TriggerStage == triggerStage
                    && r.EffectiveFrom <= asOfDate
                    && (r.EffectiveTo == null || r.EffectiveTo >= asOfDate))
                .OrderBy(r => r.Priority)
                .ToListAsync(cancellationToken);

            return rules;
        }
    }
}
