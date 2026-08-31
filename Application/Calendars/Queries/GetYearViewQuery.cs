namespace Application.Calendars.Queries
{
    public class GetYearViewQuery
    {
        public int Year { get; set; }

        // "BS" (default) or "AD" -- which calendar Year refers to.
        public string Mode { get; set; } = "BS";
    }
}
