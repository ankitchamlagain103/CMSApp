namespace Application.FeeGenerationRuns.Dtos
{
    public class FeeGenerationRunDetailDto : FeeGenerationRunDto
    {
        // Who last triggered a regenerate/refresh (UpdatedBy) and when (UpdatedTs) -- null until
        // the first regenerate/refresh call for this run.
        public string UpdatedBy { get; set; }
        public DateTimeOffset? UpdatedTs { get; set; }

        // Per-class rollups only -- no nested students/invoices. Drill into one class via
        // GET /api/feegenerationruns/{id}/classes/{academicClassId}.
        public List<FeeGenerationClassSummaryDto> Classes { get; set; } = new List<FeeGenerationClassSummaryDto>();
    }
}
