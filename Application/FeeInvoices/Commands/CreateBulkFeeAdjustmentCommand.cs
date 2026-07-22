using Domain.Enums;

namespace Application.FeeInvoices.Commands
{
    // Bulk version of CreateFeeAdjustmentCommand (2026-07-17): stamps the same Pending
    // adjustment onto every Enrolled enrollment in scope for one billing month, instead of one
    // API call per student -- the "Education Tour Fee for the whole of Grade 9" / "Examination
    // Fee for every student this term" case. Same scope shape as GenerateFeeInvoicesCommand
    // (AcademicClassId = one grade/all sections, ClassSectionId narrows further, both omitted =
    // the whole academic year).
    public class CreateBulkFeeAdjustmentCommand
    {
        public Guid AcademicYearId { get; set; }
        public Guid? AcademicClassId { get; set; }
        public Guid? ClassSectionId { get; set; }
        public int BillingYear { get; set; }
        public int BillingMonth { get; set; }
        public string AdjustmentTypeCode { get; set; }
        public string FeeCategoryCode { get; set; }
        public AdjustmentDirection Direction { get; set; }
        public AwardValueType ValueType { get; set; }
        public decimal Value { get; set; }
        public string Remarks { get; set; }
    }
}
