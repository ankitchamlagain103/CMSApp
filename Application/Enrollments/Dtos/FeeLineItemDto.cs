using Domain.Enums;

namespace Application.Enrollments.Dtos
{
    // One row of the enrollment's priced fee breakdown -- the class's FeeStructureItem row, plus
    // whether it actually applies to THIS enrollment. Mandatory items always apply; optional items
    // apply only if the enrollment opted in (EnrollmentFeeSelection, keyed by FeeStructureItemId).
    public class FeeLineItemDto
    {
        public Guid FeeStructureItemId { get; set; }
        public string FeeCategoryCode { get; set; }
        public decimal Amount { get; set; }
        public FeeFrequencyType FrequencyType { get; set; }
        public bool IsOptional { get; set; }
        public bool IsRefundable { get; set; }
        public bool Applies { get; set; }
    }
}
