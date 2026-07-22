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

        // Human-readable FeeCategory catalog label (2026-07-19); falls back to the code when
        // the option no longer exists in the catalog.
        public string FeeCategoryLabel { get; set; }
        public decimal Amount { get; set; }
        public FeeFrequencyType FrequencyType { get; set; }

        // Annual items only (2026-07-17): when set to 2+, this item is actually billed as N
        // monthly installments, not once a year -- the fee summary's MonthlyRecurringTotal
        // factors this in instead of silently under-reporting the true monthly cost.
        public int? InstallmentCount { get; set; }
        public bool IsOptional { get; set; }
        public bool IsRefundable { get; set; }
        public bool Applies { get; set; }
    }
}
