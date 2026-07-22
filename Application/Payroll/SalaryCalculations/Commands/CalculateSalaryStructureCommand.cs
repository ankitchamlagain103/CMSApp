using Domain.Enums;

namespace Application.Payroll.SalaryCalculations.Commands
{
    // Input to the HR salary calculator: fix one figure (net take-home, payable gross, or CTC)
    // and solve the rest of the structure from it. Amount is MONTHLY. FiscalYearId picks whose
    // tax slabs apply (null = the fiscal year marked current).
    //
    // Structure knobs:
    //   - BasicSalaryAmount: pin Basic to an exact monthly amount; when null, Basic is
    //     BasicPercentOfGross% of the gross (default 60).
    //   - IncludeSsf: SSF employee deduction + employer contribution lines (rates from Config
    //     catalog 1018 -- statutorily 11% / 20% of Basic).
    //   - AnnualBonusAmount: a once-a-year taxable bonus (Dashain/festival bonus) -- taxed in
    //     the annual calculation, excluded from the monthly cash figures.
    //   - MonthlyCitAmount: an optional Citizen Investment Trust savings deduction (employee-
    //     funded, retirement-flagged, so it feeds the retirement exemption).
    //   - AnnualLifeInsurancePremium / AnnualHealthInsurancePremium: tax-deductible up to each
    //     type's cap from Config catalog 1015 (LIFE 40,000 / HEALTH 20,000 as seeded).
    public class CalculateSalaryStructureCommand
    {
        public SalaryCalculationBasis Basis { get; set; }
        public decimal Amount { get; set; }
        public Guid? FiscalYearId { get; set; }
        public TaxAssessmentType AssessmentType { get; set; } = TaxAssessmentType.Individual;
        public decimal? BasicSalaryAmount { get; set; }
        public decimal? BasicPercentOfGross { get; set; }
        public bool IncludeSsf { get; set; } = true;
        public decimal? AnnualBonusAmount { get; set; }
        public decimal? MonthlyCitAmount { get; set; }
        public decimal? AnnualLifeInsurancePremium { get; set; }
        public decimal? AnnualHealthInsurancePremium { get; set; }
    }
}
