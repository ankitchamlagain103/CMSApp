namespace Domain.Constants
{
    // The third segment of the composite AdditionalValue1 format on SalaryComponentType (1013) /
    // DeductionType (1014) Config options -- see SalaryLineCalculationModes for the full format.
    // String twin of Domain.Enums.PayFrequencyType, restricted to the two values a catalog can
    // lock a line item to (a component/deduction is never catalog-locked to Annual -- that
    // remains a free-form per-line choice for a code that carries no rule at all).
    public static class SalaryLineFrequencyCodes
    {
        public const string Monthly = "MONTHLY";
        public const string OneTime = "ONE_TIME";
    }
}
