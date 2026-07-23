using Application.Payroll.Dtos;
using Domain.Entities;

namespace Application.Common.Helpers
{
    // Turns InsuranceType (Config catalog 1015) options into a code -> InsuranceCapConfig
    // dictionary -- shared by EmployeeService/PayrollRunService/SalaryCalculatorService, which
    // previously each hand-rolled the same AdditionalValue1 parse independently.
    public static class InsuranceCapHelper
    {
        public static Dictionary<string, InsuranceCapConfig> BuildCapMap(IReadOnlyList<Config> insuranceTypeOptions)
        {
            var caps = new Dictionary<string, InsuranceCapConfig>();
            foreach (var option in insuranceTypeOptions)
            {
                if (!decimal.TryParse(option.AdditionalValue1, out var capAmount))
                {
                    continue;
                }

                var eligiblePercentage = 100m;
                if (!string.IsNullOrWhiteSpace(option.AdditionalValue2) && decimal.TryParse(option.AdditionalValue2, out var parsedPercentage))
                {
                    eligiblePercentage = parsedPercentage;
                }

                caps[option.Code] = new InsuranceCapConfig
                {
                    Cap = capAmount,
                    EligiblePercentage = eligiblePercentage
                };
            }

            return caps;
        }
    }
}
