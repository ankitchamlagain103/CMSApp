namespace Application.Calendars.Commands
{
    // Bulk upsert of BS month lengths -- the yearly admin task when the government publishes
    // a new BS year's official month lengths (typically 12 rows at once).
    public class UpsertBsMonthLengthsCommand
    {
        public List<BsMonthLengthInput> Items { get; set; } = new List<BsMonthLengthInput>();
    }
}
