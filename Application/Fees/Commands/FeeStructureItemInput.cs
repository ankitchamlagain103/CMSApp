using Domain.Enums;

namespace Application.Fees.Commands
{
    // Used both as a nested item on CreateFeeStructureCommand (creating a full class fee list in
    // one call) and as the standalone body for adding one item to an existing fee structure.
    // FeeCategoryCode must be a known Config catalog code (ConfigTypeCodes.FeeCategory) -- add a
    // new one via POST /api/configs first if it isn't in the catalog yet.
    public class FeeStructureItemInput
    {
        public string FeeCategoryCode { get; set; }
        public decimal Amount { get; set; }
        public FeeFrequencyType FrequencyType { get; set; }

        // Annual items only: split the amount over this many monthly installments (2-12).
        // Omit (or 1) to charge the full amount on the enrollment's first invoice.
        public int? InstallmentCount { get; set; }
        public bool IsOptional { get; set; }
        public bool IsRefundable { get; set; }
    }
}
