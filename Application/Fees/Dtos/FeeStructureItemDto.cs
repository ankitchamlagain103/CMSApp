using Domain.Enums;

namespace Application.Fees.Dtos
{
    public class FeeStructureItemDto
    {
        public Guid Id { get; set; }
        public Guid FeeStructureId { get; set; }
        public string FeeCategoryCode { get; set; }

        // Human-readable FeeCategory catalog label (2026-07-19); falls back to the code when
        // the option no longer exists in the catalog.
        public string FeeCategoryLabel { get; set; }
        public decimal Amount { get; set; }
        public FeeFrequencyType FrequencyType { get; set; }
        public int? InstallmentCount { get; set; }
        public bool IsOptional { get; set; }
        public bool IsRefundable { get; set; }
    }
}
