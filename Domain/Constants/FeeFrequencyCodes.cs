namespace Domain.Constants
{
    // The normative fee_frequency values a FeeCategory Config option (TypeCode
    // ConfigTypeCodes.FeeCategory) carries in AdditionalValue1 -- this is the category-level
    // default that drives fee generation whenever a FeeStructureItem doesn't override it.
    // Maps 1:1 onto Domain/Enums/FeeFrequencyType; same constants-not-inline-literals shape
    // as MenuTypes/MenuAudience.
    public static class FeeFrequencyCodes
    {
        public const string Monthly = "MONTHLY";
        public const string Annual = "ANNUAL";
        public const string OneTime = "ONE_TIME";

        public static readonly string[] All = { Monthly, Annual, OneTime };
    }
}
