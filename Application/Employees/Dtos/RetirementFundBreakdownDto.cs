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
    }
}
