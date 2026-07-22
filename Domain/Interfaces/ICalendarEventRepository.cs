using Domain.Common;
using Domain.Common.Filters;
using Domain.Entities;

namespace Domain.Interfaces
{
    // Aggregate repository for calendar observances. CalendarEvent is the primary entity;
    // FestivalOccurrence rows are owned here too (both are read together by the month-view
    // query, so they form one aggregate for calendar rendering purposes).
    public interface ICalendarEventRepository : IRepository<CalendarEvent, Guid>
    {
        Task<PagedResult<CalendarEvent>> GetPagedByFilterAsync(CalendarEventFilter filter, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<CalendarEvent>> GetActiveByAdDateRangeAsync(DateTime fromAdDate, DateTime toAdDate, CancellationToken cancellationToken = default);

        Task<FestivalOccurrence> GetFestivalByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<FestivalOccurrence>> GetFestivalsAsync(int? bsYear = null, bool? isActive = null, CancellationToken cancellationToken = default);

        // Festivals whose AD span overlaps the range (start <= rangeEnd && end >= rangeStart).
        Task<IReadOnlyList<FestivalOccurrence>> GetActiveFestivalsByAdDateRangeAsync(DateTime fromAdDate, DateTime toAdDate, CancellationToken cancellationToken = default);

        Task<bool> FestivalExistsAsync(string festivalName, int bsYear, CancellationToken cancellationToken = default);

        Task AddFestivalAsync(FestivalOccurrence festival, CancellationToken cancellationToken = default);

        void RemoveFestival(FestivalOccurrence festival);
    }
}
