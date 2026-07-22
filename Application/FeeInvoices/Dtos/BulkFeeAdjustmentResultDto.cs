namespace Application.FeeInvoices.Dtos
{
    // What a bulk adjustment run did -- same "did + skipped, with reasons" shape as
    // FeeGenerationResultDto/CloneStructureResultDto.
    public class BulkFeeAdjustmentResultDto
    {
        public int BillingYear { get; set; }
        public int BillingMonth { get; set; }
        public int CreatedCount { get; set; }
        public int SkippedCount { get; set; }
        public List<Guid> CreatedAdjustmentIds { get; set; } = new List<Guid>();
        public List<FeeGenerationSkipDto> Skipped { get; set; } = new List<FeeGenerationSkipDto>();
    }
}
