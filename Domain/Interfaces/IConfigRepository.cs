using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IConfigRepository : IRepository<Config, Guid>
    {
        Task<IReadOnlyList<Config>> GetByTypeCodeAsync(int typeCode, CancellationToken cancellationToken = default);

        // Generalized dropdown filter (2026-07-24): parentCode filters on AdditionalValue1 (the
        // parent-code convention every hierarchical catalog uses -- District's own ProvinceCode,
        // LocalLevel's own DistrictCode, ...), search does a case-insensitive Label match. Either
        // or both may be null; both null behaves exactly like the plain GetByTypeCodeAsync above.
        Task<IReadOnlyList<Config>> GetByTypeCodeAsync(int typeCode, string parentCode, string search, CancellationToken cancellationToken = default);

        Task<Config> GetByTypeCodeAndCodeAsync(int typeCode, string code, CancellationToken cancellationToken = default);

        Task<bool> CodeExistsAsync(int typeCode, string code, CancellationToken cancellationToken = default);

        Task<bool> AnyByTypeCodeAsync(int typeCode, CancellationToken cancellationToken = default);
    }
}
