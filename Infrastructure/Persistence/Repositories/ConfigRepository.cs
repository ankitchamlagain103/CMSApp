using Domain.Entities;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories
{
    public class ConfigRepository : Repository<Config, Guid>, IConfigRepository
    {
        public ConfigRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<IReadOnlyList<Config>> GetByTypeCodeAsync(int typeCode, CancellationToken cancellationToken = default)
        {
            var configs = await DbSet
                .Where(config => config.TypeCode == typeCode)
                .OrderBy(config => config.Order)
                .ToListAsync(cancellationToken);

            return configs;
        }

        public async Task<Config> GetByTypeCodeAndCodeAsync(int typeCode, string code, CancellationToken cancellationToken = default)
        {
            var config = await DbSet
                .FirstOrDefaultAsync(c => c.TypeCode == typeCode && c.Code == code, cancellationToken);

            return config;
        }

        public async Task<bool> CodeExistsAsync(int typeCode, string code, CancellationToken cancellationToken = default)
        {
            var codeExists = await DbSet.AnyAsync(config => config.TypeCode == typeCode && config.Code == code, cancellationToken);
            return codeExists;
        }

        public async Task<bool> AnyByTypeCodeAsync(int typeCode, CancellationToken cancellationToken = default)
        {
            var anyExists = await DbSet.AnyAsync(config => config.TypeCode == typeCode, cancellationToken);
            return anyExists;
        }
    }
}
