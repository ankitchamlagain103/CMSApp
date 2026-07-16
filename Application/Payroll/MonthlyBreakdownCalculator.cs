using Application.Payroll.Dtos;
using Domain.Entities;
using Domain.Enums;

namespace Application.Payroll
{
    // Resolves a salary's components/deductions into 12 fiscal-month rows -- shared by the Tax
    // Details monthly table and the Payslip list/detail, so the two views can never disagree on
    // what a given month's numbers are. Pure/static, same shape as TaxCalculator, and reuses its
    // ResolveAmount/FindBasicPeriodAmount helpers so a percentage component resolves identically
    // in both the annual and the monthly view.
    public static class MonthlyBreakdownCalculator
    {
        private static readonly string[] MonthNames =
        {
            "Shrawan", "Bhadra", "Ashwin", "Kartik", "Mangsir", "Poush",
            "Magh", "Falgun", "Chaitra", "Baishakh", "Jestha", "Ashad"
        };

        public static List<MonthlyBreakdownRowDto> Build(
            IReadOnlyList<EmployeeSalaryComponent> components,
            IReadOnlyList<EmployeeSalaryDeduction> deductions,
            DateTime salaryEffectiveFromDate,
            FiscalYear fiscalYear,
            decimal annualTax)
        {
            var basicPeriodAmount = TaxCalculator.FindBasicPeriodAmount(components);
            var monthTax = annualTax / 12m;
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
                    var lineAmount = ResolveMonthAmount(component.ValueType, component.Value, component.FrequencyType, basicPeriodAmount, salaryEffectiveFromDate, periodStart, periodEnd);
                    if (lineAmount != 0m)
                    {
                        row.IncomeLines.Add(new MonthlyLineItemDto { Code = component.ComponentCode, Amount = lineAmount });
                        row.MonthGrossIncome += lineAmount;
                    }
                }

                foreach (var deduction in deductions)
                {
                    var lineAmount = ResolveMonthAmount(deduction.ValueType, deduction.Value, deduction.FrequencyType, basicPeriodAmount, salaryEffectiveFromDate, periodStart, periodEnd);
                    if (lineAmount != 0m)
                    {
                        row.DeductionLines.Add(new MonthlyLineItemDto { Code = deduction.DeductionCode, Amount = lineAmount });
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
        private static decimal ResolveMonthAmount(AwardValueType valueType, decimal value, PayFrequencyType frequencyType, decimal basicPeriodAmount, DateTime effectiveFromDate, DateTime periodStart, DateTime periodEnd)
        {
            var periodAmount = TaxCalculator.ResolveAmount(valueType, value, basicPeriodAmount);

            if (frequencyType == PayFrequencyType.Monthly)
            {
                return periodAmount;
            }

            if (frequencyType == PayFrequencyType.Annual)
            {
                return periodAmount / 12m;
            }

            var isEffectiveDateInPeriod = effectiveFromDate.Date >= periodStart && effectiveFromDate.Date <= periodEnd;
            return isEffectiveDateInPeriod ? periodAmount : 0m;
        }
    }
}
