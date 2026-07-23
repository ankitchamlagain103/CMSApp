namespace Domain.Constants
{
    // The first segment of the composite AdditionalValue1 format on SalaryComponentType (1013) /
    // DeductionType (1014) / SalaryAdjustmentType (1016) Config options -- see
    // SalaryLineCalculationModes for the full "CALCULATE_TYPE|TYPE|FREQUENCY" format. Whether the
    // code adds to gross pay or reduces it. For 1013/1014 this is inherent to which catalog the
    // code lives in (every SalaryComponentType option is Addition, every DeductionType option is
    // Deduction) -- SalaryLineCalculationHelper.ValidateCalculateType is a structural guard
    // against a code seeded into the wrong catalog, not a runtime dispatch. For 1016
    // (SalaryAdjustmentType) it replaces the old plain "EARNING"/"DEDUCTION" suggested-direction
    // value with the same vocabulary the other two catalogs use.
    public static class SalaryLineCalculateTypes
    {
        public const string Addition = "ADDITION";
        public const string Deduction = "DEDUCTION";
    }
}
