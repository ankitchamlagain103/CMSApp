using Domain.Common;
using Domain.Common.Filters;
using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IGuardianRepository : IRepository<Guardian, Guid>
    {
        Task<PagedResult<Guardian>> GetPagedByFilterAsync(GuardianFilter filter, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

        Task<bool> HasStudentLinksAsync(Guid guardianId, CancellationToken cancellationToken = default);
    }
}
