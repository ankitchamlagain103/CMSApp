using Domain.Entities;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories
{
    public class CalendarConfigRepository : Repository<BsMonthLength, Guid>, ICalendarConfigRepository
    {
        private readonly ApplicationDbContext _calendarDbContext;

        public CalendarConfigRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
            _calendarDbContext = dbContext;
        }

        public async Task<IReadOnlyList<BsMonthLength>> GetMonthLengthsAsync(int? bsYear = null, CancellationToken cancellationToken = default)
        {
            IQueryable<BsMonthLength> lengthsQuery = DbSet;

            if (bsYear.HasValue)
            {
                lengthsQuery = lengthsQuery.Where(m => m.BsYear == bsYear.Value);
            }

            var lengths = await lengthsQuery
                .OrderBy(m => m.BsYear)
                .ThenBy(m => m.BsMonth)
                .ToListAsync(cancellationToken);

            return lengths;
        }

        public async Task<BsMonthLength> GetMonthLengthAsync(int bsYear, int bsMonth, CancellationToken cancellationToken = default)
        {
            var length = await DbSet
                .FirstOrDefaultAsync(m => m.BsYear == bsYear && m.BsMonth == bsMonth, cancellationToken);

            return length;
        }

        public async Task<IReadOnlyList<BsMonthLength>> GetAllMonthLengthsOrderedAsync(CancellationToken cancellationToken = default)
        {
            var lengths = await DbSet
                .AsNoTracking()
                .OrderBy(m => m.BsYear)
                .ThenBy(m => m.BsMonth)
                .ToListAsync(cancellationToken);

            return lengths;
        }

        public async Task<IReadOnlyList<BsMonthName>> GetMonthNamesAsync(CancellationToken cancellationToken = default)
        {
            var monthNames = await _calendarDbContext.Set<BsMonthName>()
                .OrderBy(m => m.MonthNumber)
                .ToListAsync(cancellationToken);

            return monthNames;
        }

        public async Task<IReadOnlyList<BsWeekdayName>> GetWeekdayNamesAsync(CancellationToken cancellationToken = default)
        {
            var weekdayNames = await _calendarDbContext.Set<BsWeekdayName>()
                .OrderBy(w => w.WeekdayIndex)
                .ToListAsync(cancellationToken);

            return weekdayNames;
        }

        public async Task<BsWeekdayName> GetWeekdayByIndexAsync(int weekdayIndex, CancellationToken cancellationToken = default)
        {
            var weekday = await _calendarDbContext.Set<BsWeekdayName>()
                .FirstOrDefaultAsync(w => w.WeekdayIndex == weekdayIndex, cancellationToken);

            return weekday;
        }
    }
}
