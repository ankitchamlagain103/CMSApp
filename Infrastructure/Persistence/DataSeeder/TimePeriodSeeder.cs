using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Persistence.DataSeeder
{
    // Seeds a baseline set of TimePeriod rows (2026-08-03) -- an illustrative full school day
    // (periods + two breaks), same "starting point, verify before real use" caution as
    // PayrollSeeder's tax slabs. Idempotent by Name, create-if-missing only (admin edits via
    // PUT /api/timeperiods/{id} survive restarts). Deliberately does NOT map any of these to a
    // class -- ClassTimePeriod mapping is a school-specific decision (which classes use which
    // periods), made by the admin via POST /api/timeperiods/map, not assumed here.
    public static class TimePeriodSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Periods and breaks interleaved in real daily order -- a school-day routine needs
            // the breaks in the same list as the teaching periods, since both occupy slots on the
            // same timeline.
            await EnsureTimePeriodAsync(dbContext, "Period 1", "08:00:00", "08:45:00", PeriodKind.Period, 1);
            await EnsureTimePeriodAsync(dbContext, "Period 2", "08:45:00", "09:30:00", PeriodKind.Period, 2);
            await EnsureTimePeriodAsync(dbContext, "Period 3", "09:30:00", "10:15:00", PeriodKind.Period, 3);
            await EnsureTimePeriodAsync(dbContext, "Period 4", "10:15:00", "11:00:00", PeriodKind.Period, 4);
            await EnsureTimePeriodAsync(dbContext, "Short Break", "11:00:00", "11:15:00", PeriodKind.Break, 5);
            await EnsureTimePeriodAsync(dbContext, "Period 5", "11:15:00", "12:00:00", PeriodKind.Period, 6);
            await EnsureTimePeriodAsync(dbContext, "Period 6", "12:00:00", "12:45:00", PeriodKind.Period, 7);
            await EnsureTimePeriodAsync(dbContext, "Lunch Break", "12:45:00", "13:15:00", PeriodKind.Break, 8);
            await EnsureTimePeriodAsync(dbContext, "Period 7", "13:15:00", "14:00:00", PeriodKind.Period, 9);
            await EnsureTimePeriodAsync(dbContext, "Period 8", "14:00:00", "14:45:00", PeriodKind.Period, 10);
        }

        private static async Task EnsureTimePeriodAsync(
            ApplicationDbContext dbContext,
            string name,
            string startTime,
            string endTime,
            PeriodKind kind,
            int order)
        {
            var exists = await dbContext.Set<TimePeriod>()
                .IgnoreQueryFilters()
                .AnyAsync(t => t.Name == name);
            if (exists)
            {
                return;
            }

            var timePeriod = new TimePeriod
            {
                Name = name,
                StartTime = TimeSpan.Parse(startTime),
                EndTime = TimeSpan.Parse(endTime),
                Kind = kind,
                Order = order
            };

            dbContext.Set<TimePeriod>().Add(timePeriod);
            await dbContext.SaveChangesAsync();
        }
    }
}
