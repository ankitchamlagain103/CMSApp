namespace Application.Promotions.Dtos
{
    public class BulkPromotionResultDto
    {
        public Guid ExamTermId { get; set; }
        public Guid FromClassSectionId { get; set; }
        public int PromotedCount { get; set; }
        public int RetainedCount { get; set; }
        public List<BulkPromotionSkipDto> Skipped { get; set; } = new List<BulkPromotionSkipDto>();
    }
}
