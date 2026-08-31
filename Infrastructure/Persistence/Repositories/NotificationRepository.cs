using Domain.Common;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories
{
    public class NotificationRepository : Repository<Notification, Guid>, INotificationRepository
    {
        public NotificationRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<PagedResult<Notification>> GetPagedByEmployeeIdAsync(Guid employeeId, bool? isRead, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            IQueryable<Notification> notificationsQuery = DbSet.Where(n => n.EmployeeId == employeeId);

            if (isRead.HasValue)
            {
                notificationsQuery = notificationsQuery.Where(n => n.IsRead == isRead.Value);
            }

            var totalCount = await notificationsQuery.CountAsync(cancellationToken);
            var skipCount = (pageNumber - 1) * pageSize;
            var items = await notificationsQuery
                .OrderByDescending(n => n.CreatedTs)
                .Skip(skipCount)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var pagedResult = new PagedResult<Notification>
            {
                Items = items,
                TotalCount = totalCount
            };

            return pagedResult;
        }

        public async Task<int> GetUnreadCountAsync(Guid employeeId, CancellationToken cancellationToken = default)
        {
            var unreadCount = await DbSet.CountAsync(n => n.EmployeeId == employeeId && !n.IsRead, cancellationToken);
            return unreadCount;
        }

        public async Task MarkAllReadAsync(Guid employeeId, CancellationToken cancellationToken = default)
        {
            var unreadNotifications = await DbSet
                .Where(n => n.EmployeeId == employeeId && !n.IsRead)
                .ToListAsync(cancellationToken);

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
            }
        }
    }
}
