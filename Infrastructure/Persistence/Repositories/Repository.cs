using Domain.Common;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories
{
    public class Repository<TEntity, TKey> : IRepository<TEntity, TKey> where TEntity : class
    {
        protected readonly ApplicationDbContext DbContext;
        protected readonly DbSet<TEntity> DbSet;

        public Repository(ApplicationDbContext dbContext)
        {
            DbContext = dbContext;
            DbSet = dbContext.Set<TEntity>();
        }

        public async Task<TEntity> GetByIdAsync(TKey id, CancellationToken cancellationToken = default)
        {
            var entity = await DbSet.FindAsync(new object[] { id }, cancellationToken);
            return entity;
        }

        public async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var entities = await DbSet.ToListAsync(cancellationToken);
            return entities;
        }

        public async Task<PagedResult<TEntity>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            var totalCount = await DbSet.CountAsync(cancellationToken);
            var skipCount = (pageNumber - 1) * pageSize;
            var items = await DbSet
                .Skip(skipCount)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var pagedResult = new PagedResult<TEntity>
            {
                Items = items,
                TotalCount = totalCount
            };

            return pagedResult;
        }

        public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            await DbSet.AddAsync(entity, cancellationToken);
        }

        public void Update(TEntity entity)
        {
            DbSet.Update(entity);
        }

        public void Remove(TEntity entity)
        {
            DbSet.Remove(entity);
        }
    }
}
