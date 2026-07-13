using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IConfigTypeRepository : IRepository<ConfigType, Guid>
    {
        Task<bool> TypeCodeExistsAsync(int typeCode, CancellationToken cancellationToken = default);
    }
}
