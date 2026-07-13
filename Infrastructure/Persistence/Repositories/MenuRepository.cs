using Domain.Common;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories
{
    public class MenuRepository : Repository<Menu, int>, IMenuRepository
    {
        public MenuRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<PagedResult<Menu>> GetPagedByFilterAsync(string menuType, string menuFor, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            IQueryable<Menu> menusQuery = DbSet;

            if (!string.IsNullOrEmpty(menuType))
            {
                menusQuery = menusQuery.Where(menu => menu.MenuType == menuType);
            }

            if (!string.IsNullOrEmpty(menuFor))
            {
                menusQuery = menusQuery.Where(menu => menu.MenuFor == menuFor);
            }

            var totalCount = await menusQuery.CountAsync(cancellationToken);
            var skipCount = (pageNumber - 1) * pageSize;
            var items = await menusQuery
                .OrderBy(menu => menu.Order)
                .ThenBy(menu => menu.Id)
                .Skip(skipCount)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var pagedResult = new PagedResult<Menu>
            {
                Items = items,
                TotalCount = totalCount
            };

            return pagedResult;
        }

        public async Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default)
        {
            // IgnoreQueryFilters: the unique DB index on "code" still contains soft-deleted rows,
            // so a code held by a deleted menu must count as taken here -- otherwise the service
            // check passes and the insert dies at the database with a unique violation (a 500
            // instead of a clean 409).
            var codeExists = await DbSet
                .IgnoreQueryFilters()
                .AnyAsync(menu => menu.Code == code, cancellationToken);

            return codeExists;
        }

        public async Task<bool> HasChildrenAsync(int id, CancellationToken cancellationToken = default)
        {
            var hasChildren = await DbSet.AnyAsync(menu => menu.ParentId == id, cancellationToken);
            return hasChildren;
        }
    }
}
