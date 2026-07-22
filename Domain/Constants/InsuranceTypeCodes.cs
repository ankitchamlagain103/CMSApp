namespace Domain.Constants
{
    // The well-known InsuranceType (Config catalog 1015) option codes the payroll code itself
    // needs to name -- the salary calculator takes life/health premium inputs and must map them
    // onto the catalog rows that carry each type's Nepal tax-deduction cap in AdditionalValue1.
    public static class InsuranceTypeCodes
    {
        public const string Life = "LIFE";
        public const string Health = "HEALTH";
        public const string Housing = "HOUSING";
    }
}
