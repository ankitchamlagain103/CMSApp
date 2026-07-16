using Domain.Enums;

namespace Application.Fees.Commands
{
    // FeeCategoryCode is immutable -- it identifies the item; remove and re-add under a
    // different category instead of moving one.
    public class UpdateFeeStructureItemCommand
    {
        public decimal Amount { get; set; }
        public FeeFrequencyType FrequencyType { get; set; }
        public bool IsOptional { get; set; }
        public bool IsRefundable { get; set; }
    }
}
