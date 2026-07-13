using Domain.Entities;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories
{
    public class ConfigTypeRepository : Repository<ConfigType, Guid>, IConfigTypeRepository
    {
        public ConfigTypeRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<bool> TypeCodeExistsAsync(int typeCode, CancellationToken cancellationToken = default)
        {
            var typeCodeExists = await DbSet.AnyAsync(configType => configType.TypeCode == typeCode, cancellationToken);
            return typeCodeExists;
        }
    }
}
