using Domain.Common;
using Domain.Common.Filters;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories
{
    public class MeetingRepository : Repository<Meeting, Guid>, IMeetingRepository
    {
        private readonly ApplicationDbContext _meetingDbContext;

        public MeetingRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
            _meetingDbContext = dbContext;
        }

        public async Task<Meeting> GetWithAttendeesAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var meeting = await DbSet
                .Include(m => m.Attendees)
                .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

            return meeting;
        }

        public async Task<PagedResult<Meeting>> GetPagedByFilterAsync(MeetingFilter filter, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            IQueryable<Meeting> meetingsQuery = DbSet.Include(m => m.Attendees);

            if (filter.FromAdDate.HasValue)
            {
                meetingsQuery = meetingsQuery.Where(m => m.AdDate >= filter.FromAdDate.Value);
            }

            if (filter.ToAdDate.HasValue)
            {
                meetingsQuery = meetingsQuery.Where(m => m.AdDate <= filter.ToAdDate.Value);
            }

            if (filter.HostUserId.HasValue)
            {
                meetingsQuery = meetingsQuery.Where(m => m.HostUserId == filter.HostUserId.Value);
            }

            var totalCount = await meetingsQuery.CountAsync(cancellationToken);
            var skipCount = (pageNumber - 1) * pageSize;
            var items = await meetingsQuery
                .OrderBy(m => m.AdDate)
                .ThenBy(m => m.StartTime)
                .Skip(skipCount)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var pagedResult = new PagedResult<Meeting>
            {
                Items = items,
                TotalCount = totalCount
            };

            return pagedResult;
        }

        public async Task<IReadOnlyList<Meeting>> GetByAdDateRangeAsync(DateTime fromAdDate, DateTime toAdDate, CancellationToken cancellationToken = default)
        {
            var meetings = await DbSet
                .Include(m => m.Attendees)
                .Where(m => m.AdDate >= fromAdDate && m.AdDate <= toAdDate)
                .OrderBy(m => m.AdDate)
                .ThenBy(m => m.StartTime)
                .ToListAsync(cancellationToken);

            return meetings;
        }

        public async Task<bool> HasHostTimeConflictAsync(Guid hostUserId, DateTime adDate, TimeSpan startTime, TimeSpan endTime, Guid? excludeMeetingId = null, CancellationToken cancellationToken = default)
        {
            // Two [start, end) blocks overlap when each starts before the other ends.
            IQueryable<Meeting> conflictsQuery = DbSet
                .Where(m => m.HostUserId == hostUserId
                    && m.AdDate == adDate
                    && startTime < m.EndTime
                    && endTime > m.StartTime);

            if (excludeMeetingId.HasValue)
            {
                conflictsQuery = conflictsQuery.Where(m => m.Id != excludeMeetingId.Value);
            }

            var conflictExists = await conflictsQuery.AnyAsync(cancellationToken);
            return conflictExists;
        }

        public async Task<MeetingAttendee> GetAttendeeByEmailAsync(Guid meetingId, string email, CancellationToken cancellationToken = default)
        {
            var attendee = await _meetingDbContext.Set<MeetingAttendee>()
                .FirstOrDefaultAsync(a => a.MeetingId == meetingId && a.Email == email, cancellationToken);

            return attendee;
        }
    }
}
