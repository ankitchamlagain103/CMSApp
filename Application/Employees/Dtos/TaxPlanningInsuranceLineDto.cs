namespace Application.Employees.Dtos
{
    // One row of the Investment & Tax Planning tab's "Insurance Deduction" table -- the raw
    // annual premium per policy, before any per-type cap is applied. The capped total lives on
    // TaxPlanningDto.InsuranceDeductionCapped (same figure TaxCalculationResultDto.
    // InsuranceDeduction already carries).
    public class TaxPlanningInsuranceLineDto
    {
        public string InsuranceTypeCode { get; set; }
        public string InsuranceTypeLabel { get; set; }
        public decimal AnnualPremiumAmount { get; set; }
    }
}
