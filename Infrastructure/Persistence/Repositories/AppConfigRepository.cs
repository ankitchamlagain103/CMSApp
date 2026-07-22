using Domain.Entities;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories
{
    public class AppConfigRepository : Repository<AppConfig, Guid>, IAppConfigRepository
    {
        public AppConfigRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<bool> ConfigParamExistsAsync(string configParam, CancellationToken cancellationToken = default)
        {
            var configParamExists = await DbSet.AnyAsync(appConfig => appConfig.ConfigParam == configParam, cancellationToken);
            return configParamExists;
        }

        public async Task<IReadOnlyList<AppConfig>> GetByGroupAsync(string configGroup, CancellationToken cancellationToken = default)
        {
            var appConfigs = await DbSet
                .Where(appConfig => appConfig.ConfigGroup == configGroup)
                .OrderBy(appConfig => appConfig.ConfigParam)
                .ToListAsync(cancellationToken);

            return appConfigs;
        }

        public async Task<IReadOnlyList<AppConfig>> GetEnabledAsync(CancellationToken cancellationToken = default)
        {
            var appConfigs = await DbSet
                .Where(appConfig => appConfig.IsEnable)
                .OrderBy(appConfig => appConfig.ConfigGroup)
                .ThenBy(appConfig => appConfig.ConfigParam)
                .ToListAsync(cancellationToken);

            return appConfigs;
        }

        public async Task<AppConfig> GetByConfigParamAsync(string configParam, CancellationToken cancellationToken = default)
        {
            var appConfig = await DbSet
                .FirstOrDefaultAsync(config => config.ConfigParam == configParam, cancellationToken);

            return appConfig;
        }
    }
}
