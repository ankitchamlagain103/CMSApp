namespace Application.FeeInvoices.Dtos
{
    // Skip reasons grouped by count instead of one row per enrollment -- regenerating an
    // already-fully-generated month used to return hundreds of near-identical per-student rows
    // ("Invoice for this month already exists...") that the frontend had no real use for.
    public class FeeGenerationSkipSummaryDto
    {
        public string Reason { get; set; }
        public int Count { get; set; }
    }
}
