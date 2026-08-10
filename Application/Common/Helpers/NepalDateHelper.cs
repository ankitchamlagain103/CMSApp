namespace Application.Common.Helpers
{
    // Nepal has a single fixed UTC+05:45 offset (no DST), so "today in Nepal" is derived
    // arithmetically instead of via a platform-dependent timezone-id lookup. Used wherever
    // the BS calendar needs a "today" (the BS date rolls over at Nepal midnight, not UTC
    // midnight).
    public static class NepalDateHelper
    {
        private static readonly TimeSpan NepalUtcOffset = new TimeSpan(5, 45, 0);

        // Full date+time-of-day in Nepal -- for callers that need "what time is it right now"
        // (e.g. the Employee Dashboard's "next class" resolution), not just the date.
        public static DateTime GetNepalNow()
        {
            return DateTime.UtcNow.Add(NepalUtcOffset);
        }

        public static DateTime GetNepalToday()
        {
            return GetNepalNow().Date;
        }
    }
}
