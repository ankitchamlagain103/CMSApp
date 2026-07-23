using Application.Common.Helpers;
using Application.Payroll.Dtos;
using Domain.Entities;
using Domain.Enums;

namespace Application.Payroll
{
    // Resolves a salary's components/deductions into 12 fiscal-month rows -- shared by the Tax
    // Details monthly table and the Payslip list/detail, so the two views can never disagree on
    // what a given month's numbers are. Pure/static, same shape as TaxCalculator, and reuses its
    // ResolveAmount/FindBasicPeriodAmount/ResolvePercentageBaseAmount helpers so a percentage
    // component resolves identically (against the same base, Basic or a catalog-named
    // alternative) in both the annual and the monthly view.
    public static class MonthlyBreakdownCalculator
    {
        private static readonly string[] MonthNames =
        {
            "Shrawan", "Bhadra", "Ashwin", "Kartik", "Mangsir", "Poush",
            "Magh", "Falgun", "Chaitra", "Baishakh", "Jestha", "Ashad"
        };

        // Shared with the persisted-payslip paths (EmployeeService/PayrollRunService) so month
        // labels stay identical whether a month is projected or generated.
        internal static string GetMonthName(int monthIndex)
        {
            return MonthNames[monthIndex - 1];
        }

        // labelsByCode (2026-07-19): merged SalaryComponentType/DeductionType Config label map;
        // each line's Label is resolved from it (falling back to the code), so every consumer
        // -- Tax Details, payslip projections, payroll-run slip lines -- shows the same
        // human-readable name instead of the raw option code.
        public static List<MonthlyBreakdownRowDto> Build(
            IReadOnlyList<EmployeeSalaryComponent> components,
            IReadOnlyList<EmployeeSalaryDeduction> deductions,
            DateTime salaryEffectiveFromDate,
            FiscalYear fiscalYear,
            decimal annualTax,
            IReadOnlyDictionary<string, string> labelsByCode = null,
            IReadOnlyDictionary<string, SalaryLineCalculationConfig> calculationConfigByCode = null)
        {
            var basicPeriodAmount = TaxCalculator.FindBasicPeriodAmount(components);
            var monthTax = Math.Round(annualTax / 12m, 2);
            var totalDays = (fiscalYear.EndDate.Date - fiscalYear.StartDate.Date).Days + 1;

            var rows = new List<MonthlyBreakdownRowDto>();
            var periodStart = fiscalYear.StartDate.Date;

            for (var monthIndex = 1; monthIndex <= 12; monthIndex++)
            {
                var periodEnd = monthIndex == 12
                    ? fiscalYear.EndDate.Date
                    : fiscalYear.StartDate.Date.AddDays((double)(Math.Round(totalDays * monthIndex / 12m) - 1));

                var row = new MonthlyBreakdownRowDto
                {
                    MonthIndex = monthIndex,
                    MonthName = MonthNames[monthIndex - 1],
                    PeriodStartDate = periodStart,
                    PeriodEndDate = periodEnd,
                    MonthDays = (periodEnd - periodStart).Days + 1
                };

                foreach (var component in components)
                {
                    var componentBaseAmount = TaxCalculator.ResolvePercentageBaseAmount(component.ComponentCode, basicPeriodAmount, components, calculationConfigByCode);
                    var lineAmount = ResolveMonthAmount(component.ValueType, component.Value, component.FrequencyType, componentBaseAmount, salaryEffectiveFromDate, periodStart, periodEnd);
                    if (lineAmount != 0m)
                    {
                        var componentLabel = ConfigLabelHelper.Resolve(labelsByCode, component.ComponentCode);
                        row.IncomeLines.Add(new MonthlyLineItemDto { Code = component.ComponentCode, Label = componentLabel, Amount = lineAmount });
                        row.MonthGrossIncome += lineAmount;

                        // An employer-funded retirement contribution (SSF/EPF employer share,
                        // IsRetirementContribution on a COMPONENT) is money deposited with the
                        // fund, not cash paid out -- it belongs in gross (it's income, and
                        // taxable when flagged so) but must not inflate net pay, so it is
                        // offset by an equal deduction line ("remitted to the fund"). The
                        // deduction line's label gets a distinguishing suffix (2026-07-22) -- an
                        // identical "SSF Contribution" appearing as both an Earning and a
                        // Deduction with no explanatory text read as a duplicate-looking UI bug,
                        // even though the numbers were always correct.
                        if (component.IsRetirementContribution)
                        {
                            row.DeductionLines.Add(new MonthlyLineItemDto { Code = component.ComponentCode, Label = componentLabel + " (Employer Share - Fund Remittance)", Amount = lineAmount });
                        }
                    }
                }

                foreach (var deduction in deductions)
                {
                    var deductionBaseAmount = TaxCalculator.ResolvePercentageBaseAmount(deduction.DeductionCode, basicPeriodAmount, components, calculationConfigByCode);
                    var lineAmount = ResolveMonthAmount(deduction.ValueType, deduction.Value, deduction.FrequencyType, deductionBaseAmount, salaryEffectiveFromDate, periodStart, periodEnd);
                    if (lineAmount != 0m)
                    {
                        row.DeductionLines.Add(new MonthlyLineItemDto { Code = deduction.DeductionCode, Label = ConfigLabelHelper.Resolve(labelsByCode, deduction.DeductionCode), Amount = lineAmount });
                    }
                }

                row.MonthTax = monthTax;

                var totalMonthDeductions = monthTax;
                foreach (var deductionLine in row.DeductionLines)
                {
                    totalMonthDeductions += deductionLine.Amount;
                }

                row.MonthNet = row.MonthGrossIncome - totalMonthDeductions;

                rows.Add(row);
                periodStart = periodEnd.AddDays(1);
            }

            return rows;
        }

        // Monthly-frequency items resolve to their period amount every row. Annual-frequency items
        // are spread evenly across all 12 rows. OneTime-frequency items (a festival bonus, leave
        // encashment, ...) are placed only in the row containing the salary's EffectiveFromDate --
        // there's no "date this was actually paid" field to key off instead, so the revision's own
        // effective date is the closest available signal.
        private static decimal ResolveMonthAmount(AwardValueType valueType, decimal value, PayFrequencyType frequencyType, decimal baseAmount, DateTime effectiveFromDate, DateTime periodStart, DateTime periodEnd)
        {
            var periodAmount = TaxCalculator.ResolveAmount(valueType, value, baseAmount);

            if (frequencyType == PayFrequencyType.Monthly)
            {
                return periodAmount;
            }

            if (frequencyType == PayFrequencyType.Annual)
            {
                return Math.Round(periodAmount / 12m, 2);
            }

            var isEffectiveDateInPeriod = effectiveFromDate.Date >= periodStart && effectiveFromDate.Date <= periodEnd;
            return isEffectiveDateInPeriod ? periodAmount : 0m;
        }
    }
}
