namespace Domain.Enums
{
    // Where a FeeInvoiceLine came from. Sources 1-3 snapshot a FeeStructureItem (per its
    // FrequencyType), 4-7 carry the lineage id of the configuration row that produced them,
    // Manual is an admin edit on a Draft invoice, and RuleDiscount is the single
    // machine-generated line kind allowed to append after finalization (payment-time fee rules).
    public enum FeeLineSource
    {
        StructureItem = 1,
        AnnualInstallment = 2,
        OneTimeCharge = 3,
        Discount = 4,
        Scholarship = 5,
        RuleDiscount = 6,
        MonthlyAdjustment = 7,
        Manual = 8
    }
}
