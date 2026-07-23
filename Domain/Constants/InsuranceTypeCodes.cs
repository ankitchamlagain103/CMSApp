namespace Domain.Constants
{
    // The well-known InsuranceType (Config catalog 1015) option codes the payroll code itself
    // needs to name -- the salary calculator takes life/health premium inputs and must map them
    // onto the catalog rows that carry each type's Nepal tax-deduction cap in AdditionalValue1.
    // Education (added 2026-07-22) isn't insurance, but it reuses the same "capped personal
    // deduction, keyed by a catalog code, entered per employee" shape (EmployeeInsurancePremium)
    // rather than getting its own table+migration -- its cap only applies to 25% of the actual
    // annual education expense, encoded in the option's AdditionalValue2 (see InsuranceCapConfig).
    public static class InsuranceTypeCodes
    {
        public const string Life = "LIFE";
        public const string Health = "HEALTH";
        public const string Housing = "HOUSING";
        public const string Education = "EDUCATION";
    }
}
