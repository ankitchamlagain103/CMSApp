namespace Domain.Constants
{
    // AdditionalValue1 on a SalaryComponentType (1013) / DeductionType (1014) / SalaryAdjustmentType
    // (1016) Config option is a composite "CALCULATE_TYPE|TYPE|FREQUENCY" value, e.g.
    // "ADDITION|FIXED|MONTHLY" or "ADDITION|PERCENTAGE|MONTHLY" -- see
    // Domain/Constants/SalaryLineCalculateTypes (segment 1), this file (segment 2), and
    // Domain/Constants/SalaryLineFrequencyCodes (segment 3). Added 2026-07-23, replacing two
    // narrower conventions in one move: 1013/1014's old bare "PERCENTAGE"/"FIXED" value, and
    // 1016's old bare "EARNING"/"DEDUCTION" suggested-direction value.
    //
    // TYPE (this segment): whether the code is a free-form fixed per-line amount (FIXED -- also
    // today's default for a code whose AdditionalValue1 doesn't parse as the 3-segment format at
    // all, e.g. blank/legacy data) or a catalog-locked percentage of another named component
    // (PERCENTAGE, AdditionalValue2 = the rate, AdditionalValue3 = the base component's code).
    // See SalaryLineCalculationHelper for the parser and the three structural guards
    // (ValidateCalculateType/ValidatePercentageLock/ValidateFrequencyLock) built on it.
    public static class SalaryLineCalculationModes
    {
        public const string Fixed = "FIXED";
        public const string Percentage = "PERCENTAGE";
    }
}
