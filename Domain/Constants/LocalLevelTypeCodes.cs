namespace Domain.Constants
{
    // The four local-level types Nepal's 2017 federal restructuring uses (LocalLevel Config
    // options, ConfigTypeCodes.LocalLevel, AdditionalValue3). Construct/compare against these
    // constants, never inline the string literals -- same convention as MenuTypes/MenuAudience.
    public static class LocalLevelTypeCodes
    {
        public const string MetropolitanCity = "METROPOLITAN_CITY";
        public const string SubMetropolitanCity = "SUB_METROPOLITAN_CITY";
        public const string Municipality = "MUNICIPALITY";
        public const string RuralMunicipality = "RURAL_MUNICIPALITY";

        public static readonly string[] All =
        {
            MetropolitanCity,
            SubMetropolitanCity,
            Municipality,
            RuralMunicipality
        };
    }
}
