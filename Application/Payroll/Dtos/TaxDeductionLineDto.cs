namespace Application.Payroll.Dtos
{
    // One row of the "Allowable Deductions" table (insurance premiums and other capped personal
    // deductions -- Life/Health/Housing insurance, Children's Education, ...): the actual annual
    // amount declared, the percentage of it that's eligible before capping (100 for a straight
    // insurance premium, less for something like Children's Education), the type's configured
    // cap, and the amount that actually reduced taxable income. Retirement contributions have
    // their own dedicated a/b/c breakdown (RetirementFundBreakdownDto) since that's a "least of
    // three" rule, not "percentage then cap" -- this list is everything else.
    public class TaxDeductionLineDto
    {
        public string Code { get; set; }
        public string Label { get; set; }
        public decimal ActualAmount { get; set; }
        public decimal EligiblePercentage { get; set; }
        public decimal CapAmount { get; set; }
        public decimal DeductedAmount { get; set; }

        // How much MORE the employee could contribute/spend on this exact type and still have
        // every extra rupee count toward the deduction -- i.e. the raw-currency headroom left
        // under the cap, converted back through EligiblePercentage (for a 25%-eligible type like
        // Children's Education, reaching NPR 1 more of deduction needs NPR 4 more of actual
        // expense). Zero once DeductedAmount already meets CapAmount. This is the "you can
        // spend NPR X more on Y to save more tax" figure -- bind it instead of computing
        // (cap - actual) client-side, which ignores EligiblePercentage.
        public decimal AdditionalAmountAvailable { get; set; }
    }
}
