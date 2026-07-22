using Domain.Common;
using Domain.Common.Filters;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories
{
    public class CalendarEventRepository : Repository<CalendarEvent, Guid>, ICalendarEventRepository
    {
        private readonly ApplicationDbContext _calendarDbContext;

        public CalendarEventRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
            _calendarDbContext = dbContext;
        }

        public async Task<PagedResult<CalendarEvent>> GetPagedByFilterAsync(CalendarEventFilter filter, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            IQueryable<CalendarEvent> eventsQuery = DbSet;

            if (filter.EventType.HasValue)
            {
                eventsQuery = eventsQuery.Where(e => e.EventType == filter.EventType.Value);
            }

            if (filter.FromAdDate.HasValue)
            {
                eventsQuery = eventsQuery.Where(e => e.AdDate >= filter.FromAdDate.Value);
            }

            if (filter.ToAdDate.HasValue)
            {
                eventsQuery = eventsQuery.Where(e => e.AdDate <= filter.ToAdDate.Value);
            }

            if (filter.BsYear.HasValue)
            {
                eventsQuery = eventsQuery.Where(e => e.BsYear == filter.BsYear.Value);
            }

            if (filter.IsActive.HasValue)
            {
                eventsQuery = eventsQuery.Where(e => e.IsActive == filter.IsActive.Value);
            }

            var totalCount = await eventsQuery.CountAsync(cancellationToken);
            var skipCount = (pageNumber - 1) * pageSize;
            var items = await eventsQuery
                .OrderBy(e => e.AdDate)
                .ThenBy(e => e.Title)
                .Skip(skipCount)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var pagedResult = new PagedResult<CalendarEvent>
            {
                Items = items,
                TotalCount = totalCount
            };

            return pagedResult;
        }

        public async Task<IReadOnlyList<CalendarEvent>> GetActiveByAdDateRangeAsync(DateTime fromAdDate, DateTime toAdDate, CancellationToken cancellationToken = default)
        {
            var events = await DbSet
                .Where(e => e.IsActive && e.AdDate >= fromAdDate && e.AdDate <= toAdDate)
                .OrderBy(e => e.AdDate)
                .ToListAsync(cancellationToken);

            return events;
        }

        public async Task<FestivalOccurrence> GetFestivalByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var festival = await _calendarDbContext.Set<FestivalOccurrence>()
                .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

            return festival;
        }

        public async Task<IReadOnlyList<FestivalOccurrence>> GetFestivalsAsync(int? bsYear = null, bool? isActive = null, CancellationToken cancellationToken = default)
        {
            IQueryable<FestivalOccurrence> festivalsQuery = _calendarDbContext.Set<FestivalOccurrence>();

            if (bsYear.HasValue)
            {
                festivalsQuery = festivalsQuery.Where(f => f.BsYear == bsYear.Value);
            }

            if (isActive.HasValue)
            {
                festivalsQuery = festivalsQuery.Where(f => f.IsActive == isActive.Value);
            }

            var festivals = await festivalsQuery
                .OrderBy(f => f.AdStartDate)
                .ToListAsync(cancellationToken);

            return festivals;
        }

        public async Task<IReadOnlyList<FestivalOccurrence>> GetActiveFestivalsByAdDateRangeAsync(DateTime fromAdDate, DateTime toAdDate, CancellationToken cancellationToken = default)
        {
            var festivals = await _calendarDbContext.Set<FestivalOccurrence>()
                .Where(f => f.IsActive && f.AdStartDate <= toAdDate && f.AdEndDate >= fromAdDate)
                .OrderBy(f => f.AdStartDate)
                .ToListAsync(cancellationToken);

            return festivals;
        }

        public async Task<bool> FestivalExistsAsync(string festivalName, int bsYear, CancellationToken cancellationToken = default)
        {
            // IgnoreQueryFilters: a soft-deleted occurrence keeps its (name, year) reserved.
            var exists = await _calendarDbContext.Set<FestivalOccurrence>()
                .IgnoreQueryFilters()
                .AnyAsync(f => f.FestivalName == festivalName && f.BsYear == bsYear, cancellationToken);

            return exists;
        }

        public async Task AddFestivalAsync(FestivalOccurrence festival, CancellationToken cancellationToken = default)
        {
            await _calendarDbContext.Set<FestivalOccurrence>().AddAsync(festival, cancellationToken);
        }

        public void RemoveFestival(FestivalOccurrence festival)
        {
            _calendarDbContext.Set<FestivalOccurrence>().Remove(festival);
        }
    }
}
