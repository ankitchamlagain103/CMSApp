using Application.Payroll.Dtos;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;

namespace Application.Payroll
{
    // Standard Nepali payroll approach: annualize the monthly gross, run it through the fiscal
    // year's progressive tax slabs (each bracket taxes only the portion of income within its own
    // range), then divide the annual tax back down to a monthly figure. Pure/static -- no I/O, no
    // persistence, easy to reason about independent of the slabs' actual values (which are
    // admin-configured, not hardcoded here).
    public static class TaxCalculator
    {
        public static TaxCalculationResultDto Calculate(decimal annualTaxableIncome, IReadOnlyList<TaxSlab> orderedTaxSlabs)
        {
            var result = new TaxCalculationResultDto
            {
                AnnualTaxableIncome = annualTaxableIncome
            };

            decimal totalTax = 0m;
            foreach (var slab in orderedTaxSlabs)
            {
                if (annualTaxableIncome <= slab.MinAmount)
                {
                    continue;
                }

                var slabCeiling = slab.MaxAmount.HasValue ? slab.MaxAmount.Value : annualTaxableIncome;
                var incomeInSlab = Math.Min(annualTaxableIncome, slabCeiling) - slab.MinAmount;
                if (incomeInSlab <= 0m)
                {
                    continue;
                }

                var taxForSlab = incomeInSlab * slab.TaxRate;
                totalTax += taxForSlab;

                var breakdownItem = new TaxSlabBreakdownDto
                {
                    MinAmount = slab.MinAmount,
                    MaxAmount = slab.MaxAmount,
                    TaxRate = slab.TaxRate,
                    TaxableAmountInSlab = incomeInSlab,
                    TaxForSlab = taxForSlab
                };
                result.Breakdown.Add(breakdownItem);
            }

            result.AnnualTax = totalTax;
            result.MonthlyTax = totalTax / 12m;
            return result;
        }

        // Full "Investment and Tax Planning" orchestration: assembles gross annual income from
        // the salary's components, applies Nepal's retirement-fund "least of three" exemption and
        // a capped insurance-premium deduction, then feeds the remainder into Calculate(...) above.
        // Percentage-valued components/deductions resolve against the sibling "BASIC" component's
        // own (Fixed) period amount -- e.g. "SSF Deduction, 31% of Basic Salary".
        //
        // This is a faithful STRUCTURAL translation of the rule, not a guaranteed byte-exact match
        // to any specific payslip: which components/deductions an admin flags IsTaxable/
        // IsRetirementContribution is a real per-school modeling choice.
        public static TaxCalculationResultDto CalculateFromSalary(
            IReadOnlyList<EmployeeSalaryComponent> components,
            IReadOnlyList<EmployeeSalaryDeduction> deductions,
            IReadOnlyList<EmployeeInsurancePremium> insurancePremiums,
            IReadOnlyDictionary<string, decimal> insuranceTypeCaps,
            decimal retirementExemptionCapAmount,
            IReadOnlyList<TaxSlab> orderedTaxSlabs)
        {
            var basicPeriodAmount = FindBasicPeriodAmount(components);

            decimal grossAnnualIncome = 0m;
            decimal retirementContributionAnnual = 0m;

            foreach (var component in components)
            {
                var periodAmount = ResolveAmount(component.ValueType, component.Value, basicPeriodAmount);
                var annualAmount = Annualize(periodAmount, component.FrequencyType);

                if (component.IsTaxable)
                {
                    grossAnnualIncome += annualAmount;
                }

                if (component.IsRetirementContribution)
                {
                    retirementContributionAnnual += annualAmount;
                }
            }

            foreach (var deduction in deductions)
            {
                if (!deduction.IsRetirementContribution)
                {
                    continue;
                }

                var periodAmount = ResolveAmount(deduction.ValueType, deduction.Value, basicPeriodAmount);
                retirementContributionAnnual += Annualize(periodAmount, deduction.FrequencyType);
            }

            // "Least of three": actual retirement contributions, 1/3 of gross annual income, and
            // the fiscal year's configured cap.
            var retirementExemption = Math.Min(retirementContributionAnnual, Math.Min(grossAnnualIncome / 3m, retirementExemptionCapAmount));

            decimal insuranceDeduction = 0m;
            foreach (var premium in insurancePremiums)
            {
                var cap = insuranceTypeCaps.TryGetValue(premium.InsuranceTypeCode, out var capValue) ? capValue : 0m;
                insuranceDeduction += Math.Min(premium.AnnualPremiumAmount, cap);
            }

            var taxableIncome = Math.Max(0m, grossAnnualIncome - retirementExemption - insuranceDeduction);

            var result = Calculate(taxableIncome, orderedTaxSlabs);
            result.GrossAnnualIncome = grossAnnualIncome;
            result.RetirementContributionAnnual = retirementContributionAnnual;
            result.RetirementExemption = retirementExemption;
            result.InsuranceDeduction = insuranceDeduction;
            return result;
        }

        // Internal (not private) so MonthlyBreakdownCalculator can resolve the same percent-of-
        // Basic period amount for its month-by-month view -- the two calculators must never
        // disagree on what a single component/deduction resolves to in a given period.
        internal static decimal ResolveAmount(AwardValueType valueType, decimal value, decimal basicPeriodAmount)
        {
            return valueType == AwardValueType.Percentage ? basicPeriodAmount * (value / 100m) : value;
        }

        private static decimal Annualize(decimal periodAmount, PayFrequencyType frequencyType)
        {
            return frequencyType == PayFrequencyType.Monthly ? periodAmount * 12m : periodAmount;
        }

        // Shared with MonthlyBreakdownCalculator: finds the sibling "BASIC" component's own
        // (Fixed) period amount that percentage-valued components/deductions resolve against.
        internal static decimal FindBasicPeriodAmount(IReadOnlyList<EmployeeSalaryComponent> components)
        {
            var basicPeriodAmount = 0m;
            foreach (var component in components)
            {
                if (component.ComponentCode == SalaryComponentCodes.Basic)
                {
                    basicPeriodAmount = component.Value;
                }
            }

            return basicPeriodAmount;
        }
    }
}
