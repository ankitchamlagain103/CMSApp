using Domain.Common;
using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IAcademicYearRepository : IRepository<AcademicYear, Guid>
    {
        Task<PagedResult<AcademicYear>> GetPagedOrderedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);

        Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<AcademicYear>> GetCurrentYearsAsync(CancellationToken cancellationToken = default);

        Task<bool> HasClassesAsync(Guid academicYearId, CancellationToken cancellationToken = default);
    }
}
