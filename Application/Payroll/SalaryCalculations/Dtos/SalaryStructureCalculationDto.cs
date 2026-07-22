using Application.Payroll.Dtos;
using Domain.Enums;

namespace Application.Payroll.SalaryCalculations.Dtos
{
    // The solved salary structure. All top-level money figures are MONTHLY unless prefixed
    // Annual. GrossPayment is the payable cash gross (what the employee's earnings lines sum
    // to); Ctc adds the employer-side SSF contribution on top. Note TaxCalculation's own
    // GrossAnnualIncome is the TAXABLE annual gross -- it includes the employer SSF contribution
    // (taxable benefit), so it is deliberately not 12 x GrossPayment.
    public class SalaryStructureCalculationDto
    {
        public SalaryCalculationBasis Basis { get; set; }
        public Guid FiscalYearId { get; set; }
        public string FiscalYearCode { get; set; }
        public TaxAssessmentType AssessmentType { get; set; }

        public decimal BasicPercentOfGross { get; set; }
        public decimal SsfEmployeeRatePercent { get; set; }
        public decimal SsfEmployerRatePercent { get; set; }

        public decimal BasicSalary { get; set; }
        public decimal OtherAllowance { get; set; }
        public decimal GrossPayment { get; set; }
        public decimal SsfEmployeeDeduction { get; set; }
        public decimal SsfEmployerContribution { get; set; }
        public decimal MonthlyCitDeduction { get; set; }
        public decimal MonthlyTax { get; set; }
        public decimal NetPayment { get; set; }
        public decimal Ctc { get; set; }

        // Once-a-year figures. AnnualBonusAmount (Dashain/festival bonus) is taxed in the annual
        // calculation but excluded from the monthly cash rows above; the annual totals include it.
        public decimal AnnualBonusAmount { get; set; }
        public decimal AnnualGrossPayment { get; set; }
        public decimal AnnualNetPayment { get; set; }
        public decimal AnnualCtc { get; set; }

        public TaxCalculationResultDto TaxCalculation { get; set; }

        public List<SuggestedSalaryLineDto> SuggestedComponents { get; set; } = new List<SuggestedSalaryLineDto>();
        public List<SuggestedSalaryLineDto> SuggestedDeductions { get; set; } = new List<SuggestedSalaryLineDto>();
        public List<SuggestedInsurancePremiumDto> SuggestedInsurancePremiums { get; set; } = new List<SuggestedInsurancePremiumDto>();
    }
}
