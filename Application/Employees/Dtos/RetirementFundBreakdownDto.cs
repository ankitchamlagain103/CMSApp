namespace Application.Employees.Dtos
{
    // The "Retirement Fund Contribution" a/b/c breakdown the Investment & Tax Planning tab shows
    // -- TaxCalculator.CalculateFromSalary already computes the "least of three" exemption
    // internally, but only exposed the final ExemptionApplied figure until now; this DTO surfaces
    // the three inputs the exemption was chosen from, computed the exact same way.
    public class RetirementFundBreakdownDto
    {
        // (a) Sum of the salary's retirement-flagged component + deduction annual amounts --
        // same value as TaxCalculationResultDto.RetirementContributionAnnual.
        public decimal EligibleContributionAnnual { get; set; }

        // (b) One third of the salary's total (taxable) annual income.
        public decimal OneThirdOfTaxableIncome { get; set; }

        // (c) The fiscal year's configured RetirementExemptionCapAmount.
        public decimal MaximumLimit { get; set; }

        // min(a, b, c) -- same value as TaxCalculationResultDto.RetirementExemption.
        public decimal ExemptionApplied { get; set; }

        // How much MORE the employee could contribute and still have every rupee of it count
        // toward the exemption -- i.e. how much headroom is left under min(b, c) beyond what's
        // already contributed (a). Capped by BOTH b and c, not just c: contributing beyond
        // min(b, c) buys no further tax benefit even if c (the fiscal year's cap) is still far
        // away, since b (1/3 of taxable income) binds first. Zero once (a) already meets or
        // exceeds min(b, c). This is the "you can contribute an additional NPR X to save more tax"
        // figure -- exposed here so the frontend binds it instead of recomputing it (a prior
        // frontend build used (c - a) alone, ignoring the (b) cap, which overstates the useful
        // additional contribution whenever b < c).
        public decimal AdditionalContributionAvailable { get; set; }
    }
}
