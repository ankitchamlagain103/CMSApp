namespace Application.Employees.Dtos
{
    // One row of the Investment & Tax Planning tab's "Insurance Deduction" table (mirrors
    // TaxCalculationResultDto.InsuranceDeductionLines, same rows, tab-facing field names) --
    // the raw annual amount declared, its eligible percentage (100 for a straight insurance
    // premium; less for something like Children's Education, which only counts 25% of the
    // actual expense), the type's cap, and what was actually deducted. Sum of DeductedAmount
    // across every row == TaxPlanningDto.InsuranceDeductionCapped (same figure
    // TaxCalculationResultDto.InsuranceDeduction already carries).
    public class TaxPlanningInsuranceLineDto
    {
        public string InsuranceTypeCode { get; set; }
        public string InsuranceTypeLabel { get; set; }
        public decimal AnnualPremiumAmount { get; set; }
        public decimal EligiblePercentage { get; set; }
        public decimal CapAmount { get; set; }
        public decimal DeductedAmount { get; set; }
        public decimal AdditionalAmountAvailable { get; set; }
    }
}
