using Domain.Common;
using Domain.Entities;

namespace Domain.Interfaces
{
    public interface INotificationRepository : IRepository<Notification, Guid>
    {
        Task<PagedResult<Notification>> GetPagedByEmployeeIdAsync(Guid employeeId, bool? isRead, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

        Task<int> GetUnreadCountAsync(Guid employeeId, CancellationToken cancellationToken = default);

        Task MarkAllReadAsync(Guid employeeId, CancellationToken cancellationToken = default);
    }
}
