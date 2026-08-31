namespace Application.Promotions.Commands
{
    // End-of-year processing for a whole section (design doc section 5.3's decision workflow):
    // every Enrolled student in FromClassSectionId is evaluated against their StudentResult for
    // ExamTermId -- Pass moves to PromotedToClassSectionId (PromotionType.Promoted), Fail/
    // Compartment moves to RetainedToClassSectionId (PromotionType.Retained). Both destination
    // sections must already exist (created via the academic-year clone-structure endpoint or
    // manually) -- this command does not infer "next grade" on its own.
    public class BulkProcessPromotionCommand
    {
        public Guid ExamTermId { get; set; }
        public Guid FromClassSectionId { get; set; }
        public Guid PromotedToClassSectionId { get; set; }
        public Guid RetainedToClassSectionId { get; set; }
        public DateTime PromotionDate { get; set; }
        public string Remarks { get; set; }
    }
}
