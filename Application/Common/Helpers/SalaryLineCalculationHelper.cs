using Application.Payroll.Dtos;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;

namespace Application.Common.Helpers
{
    // Parses the composite "CALCULATE_TYPE|TYPE|FREQUENCY" AdditionalValue1 format (2026-07-23,
    // Domain/Constants/SalaryLineCalculationModes) on SalaryComponentType/DeductionType/
    // SalaryAdjustmentType catalog options, e.g. "ADDITION|FIXED|MONTHLY" or
    // "ADDITION|PERCENTAGE|MONTHLY" (AdditionalValue2 = the rate, AdditionalValue3 = the base
    // component's code, only meaningful when TYPE == PERCENTAGE). A value that's blank or doesn't
    // parse as exactly this 3-segment shape means "this code carries no catalog-level rule at
    // all" -- every check below then returns null (no constraint), which is what keeps a
    // not-yet-migrated catalog option, or one an admin adds without following the convention, on
    // fully free-form per-line entry.
    public static class SalaryLineCalculationHelper
    {
        public static Dictionary<string, SalaryLineCalculationConfig> BuildConfigMap(IReadOnlyList<Config> options)
        {
            var configByCode = new Dictionary<string, SalaryLineCalculationConfig>();
            MergeConfigMap(configByCode, options);
            return configByCode;
        }

        public static void MergeConfigMap(Dictionary<string, SalaryLineCalculationConfig> configByCode, IReadOnlyList<Config> options)
        {
            foreach (var option in options)
            {
                var rule = ParseRule(option.AdditionalValue1);
                if (rule == null)
                {
                    continue;
                }

                var config = new SalaryLineCalculationConfig
                {
                    CalculateType = rule.Value.CalculateType,
                    Mode = rule.Value.Mode,
                    Frequency = rule.Value.Frequency
                };

                if (rule.Value.Mode == SalaryLineCalculationModes.Percentage && decimal.TryParse(option.AdditionalValue2, out var rate))
                {
                    config.Rate = rate;
                    config.BaseComponentCode = string.IsNullOrWhiteSpace(option.AdditionalValue3) ? SalaryComponentCodes.Basic : option.AdditionalValue3.Trim();
                }

                configByCode[option.Code] = config;
            }
        }

        // Structural guard against exactly the mistake that prompted this feature (SSF_DEDUCTION
        // hand-entered at 31% instead of 11%): when a catalog option is percentage-locked
        // (TYPE == PERCENTAGE), a submitted line for that code must be Percentage-valued and
        // match the catalog's own rate exactly -- null return means no constraint (catalog option
        // is Fixed/carries no rule, today's free-form behavior).
        public static string ValidatePercentageLock(Config catalogOption, AwardValueType valueType, decimal value)
        {
            var rule = ParseRule(catalogOption.AdditionalValue1);
            if (rule == null || rule.Value.Mode != SalaryLineCalculationModes.Percentage || !decimal.TryParse(catalogOption.AdditionalValue2, out var requiredRate))
            {
                return null;
            }

            if (valueType == AwardValueType.Percentage && value == requiredRate)
            {
                return null;
            }

            var baseCode = string.IsNullOrWhiteSpace(catalogOption.AdditionalValue3) ? SalaryComponentCodes.Basic : catalogOption.AdditionalValue3.Trim();
            return "'" + catalogOption.Code + "' is locked to Percentage, " + requiredRate + "% (of " + baseCode + ") by its catalog configuration -- it cannot be entered as a different value or as a Fixed amount.";
        }

        // Structural guard, same shape as ValidatePercentageLock. Two call shapes:
        // - SalaryComponentType (1013) / DeductionType (1014): a 1013 option's CALCULATE_TYPE
        //   must be ADDITION and a 1014 option's must be DEDUCTION -- catches a catalog row
        //   seeded/edited into the wrong table's namespace. Callers pass the catalog type they
        //   looked the code up in as expectedCalculateType, so this can never fire from correct
        //   catalog usage alone -- it's a defense against bad seed/admin data, not a runtime
        //   redirect.
        // - SalaryAdjustmentType (1016): callers pass the CalculateType implied by the submitted
        //   Direction (Increase -> ADDITION, Decrease -> DEDUCTION), so a "LATE_FINE" adjustment
        //   catalogued as DEDUCTION can't be recorded as Direction = Increase.
        public static string ValidateCalculateType(Config catalogOption, string expectedCalculateType)
        {
            var rule = ParseRule(catalogOption.AdditionalValue1);
            if (rule == null || rule.Value.CalculateType == expectedCalculateType)
            {
                return null;
            }

            return "'" + catalogOption.Code + "' is catalogued as " + rule.Value.CalculateType + ", not " + expectedCalculateType + " -- check its catalog configuration.";
        }

        // Structural guard, same shape as ValidatePercentageLock: when a catalog option locks a
        // FREQUENCY, a submitted line for that code must use exactly that frequency -- prevents,
        // e.g., a "Festival Bonus" (catalog-locked ONE_TIME) from being entered as a recurring
        // MONTHLY line, or a recurring allowance from being entered as a one-off.
        public static string ValidateFrequencyLock(Config catalogOption, PayFrequencyType frequencyType)
        {
            var rule = ParseRule(catalogOption.AdditionalValue1);
            if (rule == null || rule.Value.Frequency == frequencyType)
            {
                return null;
            }

            return "'" + catalogOption.Code + "' is locked to " + rule.Value.Frequency + " frequency by its catalog configuration.";
        }

        // The catalog's own locked frequency, or null when the option carries no rule -- used
        // where a line is synthesized rather than caller-supplied (PayrollRunService's manual
        // slip-line-to-structure sync), so the inserted line's frequency matches the catalog
        // instead of a hardcoded guess.
        public static PayFrequencyType? ResolveFrequency(Config catalogOption)
        {
            var rule = ParseRule(catalogOption.AdditionalValue1);
            return rule?.Frequency;
        }

        private static (string CalculateType, string Mode, PayFrequencyType Frequency)? ParseRule(string additionalValue1)
        {
            if (string.IsNullOrWhiteSpace(additionalValue1))
            {
                return null;
            }

            var segments = additionalValue1.Split('|');
            if (segments.Length != 3)
            {
                return null;
            }

            var calculateType = segments[0].Trim().ToUpperInvariant();
            if (calculateType != SalaryLineCalculateTypes.Addition && calculateType != SalaryLineCalculateTypes.Deduction)
            {
                return null;
            }

            var mode = segments[1].Trim().ToUpperInvariant();
            if (mode != SalaryLineCalculationModes.Fixed && mode != SalaryLineCalculationModes.Percentage)
            {
                return null;
            }

            var frequencyCode = segments[2].Trim().ToUpperInvariant();
            PayFrequencyType frequency;
            if (frequencyCode == SalaryLineFrequencyCodes.Monthly)
            {
                frequency = PayFrequencyType.Monthly;
            }
            else if (frequencyCode == SalaryLineFrequencyCodes.OneTime)
            {
                frequency = PayFrequencyType.OneTime;
            }
            else
            {
                return null;
            }

            return (calculateType, mode, frequency);
        }
    }
}
