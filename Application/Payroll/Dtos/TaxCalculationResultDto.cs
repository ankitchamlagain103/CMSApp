namespace Application.Payroll.Dtos
{
    public class TaxCalculationResultDto
    {
        // Investment/tax-planning breakdown (populated by TaxCalculator.CalculateFromSalary;
        // zero/default when the plain slab-only Calculate(...) overload is used directly).
        public decimal GrossAnnualIncome { get; set; }
        public decimal RetirementContributionAnnual { get; set; }
        public decimal RetirementExemption { get; set; }
        public decimal InsuranceDeduction { get; set; }

        public decimal AnnualTaxableIncome { get; set; }
        public decimal AnnualTax { get; set; }
        public decimal MonthlyTax { get; set; }
        public List<TaxSlabBreakdownDto> Breakdown { get; set; } = new List<TaxSlabBreakdownDto>();
    }
}
