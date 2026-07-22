namespace Application.Common.Helpers
{
    // Nepal has a single fixed UTC+05:45 offset (no DST), so "today in Nepal" is derived
    // arithmetically instead of via a platform-dependent timezone-id lookup. Used wherever
    // the BS calendar needs a "today" (the BS date rolls over at Nepal midnight, not UTC
    // midnight).
    public static class NepalDateHelper
    {
        private static readonly TimeSpan NepalUtcOffset = new TimeSpan(5, 45, 0);

        public static DateTime GetNepalToday()
        {
            var nepalNow = DateTime.UtcNow.Add(NepalUtcOffset);
            return nepalNow.Date;
        }
    }
}
