namespace Application.Payroll.SalaryCalculations.Dtos
{
    // One ready-to-use insurance premium line -- shaped like the InsurancePremiums input of
    // POST /api/employees/{id}/salaries, same prefill purpose as SuggestedSalaryLineDto.
    public class SuggestedInsurancePremiumDto
    {
        public string InsuranceTypeCode { get; set; }
        public decimal AnnualPremiumAmount { get; set; }
    }
}
