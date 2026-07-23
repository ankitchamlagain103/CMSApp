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

        // Per-type breakdown behind InsuranceDeduction -- one row per EmployeeInsurancePremium on
        // the salary (Life/Health/Housing insurance, Children's Education, ...), each showing the
        // actual amount declared, its eligible percentage, its cap, and what was actually
        // deducted. Sum of DeductedAmount across this list == InsuranceDeduction.
        public List<TaxDeductionLineDto> InsuranceDeductionLines { get; set; } = new List<TaxDeductionLineDto>();

        public decimal AnnualTaxableIncome { get; set; }
        public decimal AnnualTax { get; set; }
        public decimal MonthlyTax { get; set; }

        // Everything that reduces actual cash take-home besides tax: every real
        // EmployeeSalaryDeduction (SSF/CIT/loan/advance, annualized) plus every
        // IsRetirementContribution component (the employer SSF/EPF share -- it's taxable income
        // but never reaches the employee's pocket, same offsetting rule
        // MonthlyBreakdownCalculator already applies per month).
        public decimal CashDeductionsAnnual { get; set; }

        // GrossAnnualIncome - CashDeductionsAnnual - AnnualTax -- the true annual take-home.
        // GrossMonthly/NetMonthly on EmployeeTaxCalculationDto are this (and GrossAnnualIncome)
        // divided by 12, not GrossAnnualIncome/12 minus only MonthlyTax.
        public decimal NetAnnualIncome { get; set; }

        // Nepal's Social Security Tax waiver: true when the salary has an active SSF
        // contribution, meaning the first tax slab's 1% is not charged (see Breakdown row's own
        // IsSsfExempted for which row that was).
        public bool IsSsfExemptionApplied { get; set; }

        public List<TaxSlabBreakdownDto> Breakdown { get; set; } = new List<TaxSlabBreakdownDto>();
    }
}
