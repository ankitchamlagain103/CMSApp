namespace Application.Common.Helpers
{
    public static class RecurringDateHelper
    {
        // The next occurrence of an annually-recurring month/day (birthday, work anniversary) on
        // or after today -- rolls into next year once this year's date has already passed. Shared
        // by EmployeeService.GetEmployeeProfileAsync (single employee) and the HR dashboard
        // summary (org-wide) so both use one implementation.
        public static DateTime ResolveNextOccurrence(DateTime anniversaryDate, DateTime today)
        {
            var day = Math.Min(anniversaryDate.Day, DateTime.DaysInMonth(today.Year, anniversaryDate.Month));
            var candidate = new DateTime(today.Year, anniversaryDate.Month, day);
            if (candidate < today)
            {
                var nextDay = Math.Min(anniversaryDate.Day, DateTime.DaysInMonth(today.Year + 1, anniversaryDate.Month));
                candidate = new DateTime(today.Year + 1, anniversaryDate.Month, nextDay);
            }

            return candidate;
        }
    }
}
