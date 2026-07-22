using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IAppConfigRepository : IRepository<AppConfig, Guid>
    {
        Task<bool> ConfigParamExistsAsync(string configParam, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<AppConfig>> GetByGroupAsync(string configGroup, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<AppConfig>> GetEnabledAsync(CancellationToken cancellationToken = default);

        Task<AppConfig> GetByConfigParamAsync(string configParam, CancellationToken cancellationToken = default);
    }
}
