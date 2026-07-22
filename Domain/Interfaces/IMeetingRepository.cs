using Domain.Common;
using Domain.Common.Filters;
using Domain.Entities;

namespace Domain.Interfaces
{
    // Aggregate repository for meetings; owns the MeetingAttendee child rows.
    public interface IMeetingRepository : IRepository<Meeting, Guid>
    {
        Task<Meeting> GetWithAttendeesAsync(Guid id, CancellationToken cancellationToken = default);

        Task<PagedResult<Meeting>> GetPagedByFilterAsync(MeetingFilter filter, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Meeting>> GetByAdDateRangeAsync(DateTime fromAdDate, DateTime toAdDate, CancellationToken cancellationToken = default);

        // True if the host already has a meeting on adDate whose [StartTime, EndTime) block
        // overlaps the given one. excludeMeetingId skips the meeting being updated.
        Task<bool> HasHostTimeConflictAsync(Guid hostUserId, DateTime adDate, TimeSpan startTime, TimeSpan endTime, Guid? excludeMeetingId = null, CancellationToken cancellationToken = default);

        Task<MeetingAttendee> GetAttendeeByEmailAsync(Guid meetingId, string email, CancellationToken cancellationToken = default);
    }
}
