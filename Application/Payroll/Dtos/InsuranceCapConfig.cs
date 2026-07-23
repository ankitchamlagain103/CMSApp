namespace Application.Payroll.Dtos
{
    // One InsuranceType (Config catalog 1015) option's tax-deduction rule: Cap comes from
    // AdditionalValue1 (unchanged); EligiblePercentage comes from AdditionalValue2 and defaults
    // to 100 when blank -- most types (Life/Health/Housing) deduct the full premium up to the
    // cap, but a type like Children's Education only lets a percentage of the actual expense
    // count before the cap applies (Nepal's "25% of annual education expense, max NPR 25,000"
    // rule). Kept in Config's free-form slots rather than hardcoded, same convention as every
    // other percentage-of-something default in this codebase (Subject credit hours, Discount/
    // ScholarshipType default rates, SSF share percentages, ...).
    public class InsuranceCapConfig
    {
        public decimal Cap { get; set; }
        public decimal EligiblePercentage { get; set; } = 100m;
    }
}
