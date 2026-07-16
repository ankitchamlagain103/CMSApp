using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IConfigRepository : IRepository<Config, Guid>
    {
        Task<IReadOnlyList<Config>> GetByTypeCodeAsync(int typeCode, CancellationToken cancellationToken = default);

        Task<Config> GetByTypeCodeAndCodeAsync(int typeCode, string code, CancellationToken cancellationToken = default);

        Task<bool> CodeExistsAsync(int typeCode, string code, CancellationToken cancellationToken = default);

        Task<bool> AnyByTypeCodeAsync(int typeCode, CancellationToken cancellationToken = default);
    }
}
