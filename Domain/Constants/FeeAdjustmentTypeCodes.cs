namespace Domain.Constants
{
    // The one FeeAdjustmentType Config code (TypeCode ConfigTypeCodes.FeeAdjustmentType) with
    // special generation-time handling: FeeInvoiceService.GenerateAsync auto-creates a Pending
    // CARRY_CORRECTION adjustment when an enrollment has an outstanding balance from strictly
    // earlier invoices, so the new invoice visibly bills the old debt forward. Every other code
    // is admin-entered only.
    public static class FeeAdjustmentTypeCodes
    {
        public const string CarryCorrection = "CARRY_CORRECTION";
    }
}
