using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Persistence.DataSeeder
{
    // Seeds a baseline set of leave types (2026-07-23, policy fields added 2026-07-24) so the
    // Leave Management feature works out of the box, same "illustrative starting point" caution
    // as PayrollSeeder's tax slabs -- verify every value against the institution's actual policy
    // before real use. Idempotent by Name, create-if-missing only (admin edits via
    // PUT /api/leavetypes survive restarts -- an already-seeded row does NOT get these policy
    // fields retroactively; edit it directly if you want them).
    public static class LeaveTypeSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Annual Leave: planned well ahead, no day-count caps.
            await EnsureLeaveTypeAsync(dbContext, "Annual Leave", 18m, true, true, null, null, null);

            // Sick Leave: unpredictable in length, so no caps of its own -- a longer stretch is
            // still just a longer Sick Leave request; whether a medical certificate backs it is
            // left to the requester/HR conversation, not a system-enforced gate.
            await EnsureLeaveTypeAsync(dbContext, "Sick Leave", 12m, false, true, null, null, null);

            // Casual Leave: the "prevent abuse" example -- capped at 3 consecutive days and 5 days
            // in any calendar month.
            await EnsureLeaveTypeAsync(dbContext, "Casual Leave", 12m, false, true, 3, null, 5);

            // Bereavement Leave (2026-07-24): a death in the immediate family. Capped at its own
            // yearly allocation in a single request -- an employee wouldn't split this across
            // multiple applications.
            await EnsureLeaveTypeAsync(dbContext, "Bereavement Leave", 13m, false, true, 13, null, null);

            // Marriage Leave (2026-07-24): planned well ahead like Annual Leave, capped at the
            // type's own DaysPerYear via MaxConsecutiveDays.
            await EnsureLeaveTypeAsync(dbContext, "Marriage Leave", 7m, false, true, 7, null, null);
        }

        private static async Task EnsureLeaveTypeAsync(
            ApplicationDbContext dbContext,
            string name,
            decimal daysPerYear,
            bool carryForward,
            bool isPaid,
            int? maxConsecutiveDays,
            decimal? maxDaysPerWeek,
            decimal? maxDaysPerMonth)
        {
            var exists = await dbContext.Set<LeaveType>()
                .IgnoreQueryFilters()
                .AnyAsync(t => t.Name == name);
            if (exists)
            {
                return;
            }

            var leaveType = new LeaveType
            {
                Name = name,
                DaysPerYear = daysPerYear,
                CarryForward = carryForward,
                IsPaid = isPaid,
                MaxConsecutiveDays = maxConsecutiveDays,
                MaxDaysPerWeek = maxDaysPerWeek,
                MaxDaysPerMonth = maxDaysPerMonth
            };

            dbContext.Set<LeaveType>().Add(leaveType);
            await dbContext.SaveChangesAsync();
        }
    }
}
