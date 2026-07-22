namespace Application.Calendars.Queries
{
    public class GetMonthViewQuery
    {
        public int Year { get; set; }
        public int Month { get; set; }

        // "BS" (default) or "AD" -- which calendar Year/Month refer to.
        public string Mode { get; set; } = "BS";
    }
}
