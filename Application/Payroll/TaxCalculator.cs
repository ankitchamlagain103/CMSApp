using Application.Common.Helpers;
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
        // isSsfContributor (2026-07-22): Nepal's Social Security Tax rule -- the 1% tax on the
        // first income slab is waived entirely for anyone contributing to the Social Security
        // Fund (they're already funding the government's own social-security scheme via SSF, so
        // the separate 1% "Social Security Tax" bracket doesn't apply on top of it). This is
        // distinct from the retirement-fund "least of three" exemption on taxable income above --
        // that reduces the taxable base; this zeroes the tax on the first bracket specifically.
        // Only the first slab (orderedTaxSlabs[0], by construction the lowest bracket) is waived;
        // any income spilling into higher slabs is taxed normally.
        public static TaxCalculationResultDto Calculate(decimal annualTaxableIncome, IReadOnlyList<TaxSlab> orderedTaxSlabs, bool isSsfContributor = false)
        {
            var result = new TaxCalculationResultDto
            {
                AnnualTaxableIncome = annualTaxableIncome,
                IsSsfExemptionApplied = isSsfContributor
            };

            decimal totalTax = 0m;
            for (var slabIndex = 0; slabIndex < orderedTaxSlabs.Count; slabIndex++)
            {
                var slab = orderedTaxSlabs[slabIndex];
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

                var isSsfExemptSlab = slabIndex == 0 && isSsfContributor;
                var taxForSlab = isSsfExemptSlab ? 0m : Math.Round(incomeInSlab * slab.TaxRate, 2);
                totalTax += taxForSlab;

                var breakdownItem = new TaxSlabBreakdownDto
                {
                    MinAmount = slab.MinAmount,
                    MaxAmount = slab.MaxAmount,
                    TaxRate = slab.TaxRate,
                    TaxableAmountInSlab = incomeInSlab,
                    TaxForSlab = taxForSlab,
                    IsSsfExempted = isSsfExemptSlab
                };
                result.Breakdown.Add(breakdownItem);
            }

            result.AnnualTax = totalTax;
            result.MonthlyTax = Math.Round(totalTax / 12m, 2);
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
            IReadOnlyDictionary<string, InsuranceCapConfig> insuranceTypeCaps,
            decimal retirementExemptionCapAmount,
            IReadOnlyList<TaxSlab> orderedTaxSlabs,
            IReadOnlyDictionary<string, string> labelsByCode = null,
            IReadOnlyDictionary<string, SalaryLineCalculationConfig> calculationConfigByCode = null)
        {
            var basicPeriodAmount = FindBasicPeriodAmount(components);

            decimal grossAnnualIncome = 0m;
            decimal retirementContributionAnnual = 0m;
            decimal cashDeductionsAnnual = 0m;
            var hasSsfContribution = false;

            foreach (var component in components)
            {
                var baseAmount = ResolvePercentageBaseAmount(component.ComponentCode, basicPeriodAmount, components, calculationConfigByCode);
                var periodAmount = ResolveAmount(component.ValueType, component.Value, baseAmount);
                var annualAmount = Annualize(periodAmount, component.FrequencyType);

                if (component.IsTaxable)
                {
                    grossAnnualIncome += annualAmount;
                }

                if (component.IsRetirementContribution)
                {
                    retirementContributionAnnual += annualAmount;

                    // An employer-funded retirement contribution (SSF/EPF employer share) is
                    // money deposited with the fund, not cash paid to the employee -- it belongs
                    // in gross/taxable income (it's a real taxable benefit) but must not inflate
                    // net take-home pay. Same offsetting rule MonthlyBreakdownCalculator already
                    // applies per month; this is its annual equivalent.
                    cashDeductionsAnnual += annualAmount;
                }

                if (component.ComponentCode == SalaryComponentCodes.SsfContribution && annualAmount > 0m)
                {
                    hasSsfContribution = true;
                }
            }

            foreach (var deduction in deductions)
            {
                var baseAmount = ResolvePercentageBaseAmount(deduction.DeductionCode, basicPeriodAmount, components, calculationConfigByCode);
                var periodAmount = ResolveAmount(deduction.ValueType, deduction.Value, baseAmount);
                var annualAmount = Annualize(periodAmount, deduction.FrequencyType);

                if (deduction.IsRetirementContribution)
                {
                    retirementContributionAnnual += annualAmount;
                }

                // Every deduction reduces cash take-home, regardless of its retirement flag
                // (SSF_DEDUCTION/CIT_DEDUCTION are retirement-flagged; a LOAN/ADVANCE deduction
                // isn't, but still comes out of pay).
                cashDeductionsAnnual += annualAmount;

                if (deduction.DeductionCode == SalaryDeductionCodes.SsfDeduction && annualAmount > 0m)
                {
                    hasSsfContribution = true;
                }
            }

            // "Least of three": actual retirement contributions, 1/3 of gross annual income, and
            // the fiscal year's configured cap.
            var retirementExemption = Math.Round(Math.Min(retirementContributionAnnual, Math.Min(grossAnnualIncome / 3m, retirementExemptionCapAmount)), 2);

            decimal insuranceDeduction = 0m;
            var insuranceDeductionLines = new List<TaxDeductionLineDto>();
            foreach (var premium in insurancePremiums)
            {
                var capConfig = insuranceTypeCaps.TryGetValue(premium.InsuranceTypeCode, out var config) ? config : new InsuranceCapConfig { Cap = 0m };

                // Most types (Life/Health/Housing) deduct the full premium up to the cap
                // (EligiblePercentage 100). A type like Children's Education only lets a
                // percentage of the actual expense count before the cap applies.
                var eligibleAmount = Math.Round(premium.AnnualPremiumAmount * (capConfig.EligiblePercentage / 100m), 2);
                var deductedAmount = Math.Round(Math.Min(eligibleAmount, capConfig.Cap), 2);
                insuranceDeduction += deductedAmount;

                var remainingEligibleHeadroom = Math.Max(0m, capConfig.Cap - deductedAmount);
                var additionalAmountAvailable = capConfig.EligiblePercentage > 0m
                    ? Math.Round(remainingEligibleHeadroom / (capConfig.EligiblePercentage / 100m), 2)
                    : 0m;

                insuranceDeductionLines.Add(new TaxDeductionLineDto
                {
                    Code = premium.InsuranceTypeCode,
                    Label = ConfigLabelHelper.Resolve(labelsByCode, premium.InsuranceTypeCode),
                    ActualAmount = premium.AnnualPremiumAmount,
                    EligiblePercentage = capConfig.EligiblePercentage,
                    CapAmount = capConfig.Cap,
                    DeductedAmount = deductedAmount,
                    AdditionalAmountAvailable = additionalAmountAvailable
                });
            }

            var taxableIncome = Math.Max(0m, grossAnnualIncome - retirementExemption - insuranceDeduction);

            var result = Calculate(taxableIncome, orderedTaxSlabs, hasSsfContribution);
            result.GrossAnnualIncome = grossAnnualIncome;
            result.RetirementContributionAnnual = retirementContributionAnnual;
            result.RetirementExemption = retirementExemption;
            result.InsuranceDeduction = insuranceDeduction;
            result.InsuranceDeductionLines = insuranceDeductionLines;
            result.CashDeductionsAnnual = cashDeductionsAnnual;
            result.NetAnnualIncome = Math.Max(0m, grossAnnualIncome - cashDeductionsAnnual - result.AnnualTax);
            return result;
        }

        // Internal (not private) so MonthlyBreakdownCalculator can resolve the same percent-of-
        // Basic period amount for its month-by-month view -- the two calculators must never
        // disagree on what a single component/deduction resolves to in a given period.
        internal static decimal ResolveAmount(AwardValueType valueType, decimal value, decimal basicPeriodAmount)
        {
            return valueType == AwardValueType.Percentage ? Math.Round(basicPeriodAmount * (value / 100m), 2) : value;
        }

        // Internal (not private) so the Investment & Tax Planning composite endpoint
        // (EmployeeService.GetTaxPlanningAsync) can annualize each component/deduction the exact
        // same way CalculateFromSalary does internally, for its per-line income breakdown.
        internal static decimal Annualize(decimal periodAmount, PayFrequencyType frequencyType)
        {
            return frequencyType == PayFrequencyType.Monthly ? periodAmount * 12m : periodAmount;
        }

        // Shared with MonthlyBreakdownCalculator: finds the sibling "BASIC" component's own
        // (Fixed) period amount that percentage-valued components/deductions resolve against
        // by default (see ResolvePercentageBaseAmount for the catalog-overridable case).
        internal static decimal FindBasicPeriodAmount(IReadOnlyList<EmployeeSalaryComponent> components)
        {
            return FindComponentPeriodAmount(components, SalaryComponentCodes.Basic);
        }

        // General form of FindBasicPeriodAmount -- looks up any named sibling component's own
        // period amount, for percentage lines whose catalog names a base other than Basic. Only
        // meaningful for a Fixed-valued base component (chaining one percentage off another
        // isn't a supported case); a Percentage-valued or missing base resolves to 0.
        internal static decimal FindComponentPeriodAmount(IReadOnlyList<EmployeeSalaryComponent> components, string code)
        {
            foreach (var component in components)
            {
                if (component.ComponentCode == code && component.ValueType == AwardValueType.FixedAmount)
                {
                    return component.Value;
                }
            }

            return 0m;
        }

        // 2026-07-22: which sibling component's period amount a Percentage-valued line resolves
        // against. Defaults to Basic (today's only behavior) unless the SalaryComponentType/
        // DeductionType catalog names a different base via AdditionalValue3 (see
        // SalaryLineCalculationHelper) -- e.g. an "Overtime = 150% of Basic" component still
        // uses Basic, but a future "Bonus = 8.33% of Gross" component could name a different
        // base without any change here. calculationConfigByCode is optional (null = always
        // Basic) so every existing caller that doesn't pass it keeps its exact prior behavior.
        internal static decimal ResolvePercentageBaseAmount(string code, decimal basicPeriodAmount, IReadOnlyList<EmployeeSalaryComponent> components, IReadOnlyDictionary<string, SalaryLineCalculationConfig> calculationConfigByCode)
        {
            if (calculationConfigByCode == null || !calculationConfigByCode.TryGetValue(code, out var config))
            {
                return basicPeriodAmount;
            }

            if (config.BaseComponentCode == SalaryComponentCodes.Basic)
            {
                return basicPeriodAmount;
            }

            return FindComponentPeriodAmount(components, config.BaseComponentCode);
        }
    }
}
