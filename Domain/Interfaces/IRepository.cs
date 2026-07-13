using Domain.Common;

namespace Domain.Interfaces
{
    public interface IRepository<TEntity, TKey> where TEntity : class
    {
        Task<TEntity> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

        Task<PagedResult<TEntity>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);

        Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

        void Update(TEntity entity);

        void Remove(TEntity entity);
    }
}
