using Domain.Entities;

namespace Application.Common.Helpers
{
    // Turns Config catalog options into a code -> Label dictionary so read DTOs and generated
    // line descriptions can carry the human-readable label alongside every stored option code
    // (pages otherwise showed raw codes like "SSF_CONTRIBUTION" or "TUITION"). Resolve falls
    // back to the code itself, so a code whose catalog option was deleted after rows referencing
    // it were written still renders as something rather than blank.
    public static class ConfigLabelHelper
    {
        public static Dictionary<string, string> BuildLabelMap(IReadOnlyList<Config> options)
        {
            var labelsByCode = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            MergeLabelMap(labelsByCode, options);
            return labelsByCode;
        }

        // First catalog wins on a code collision -- callers merge related catalogs (e.g. salary
        // components + deductions) into one map, and their codes are namespaced by convention.
        public static void MergeLabelMap(Dictionary<string, string> labelsByCode, IReadOnlyList<Config> options)
        {
            foreach (var option in options)
            {
                if (string.IsNullOrWhiteSpace(option.Code) || labelsByCode.ContainsKey(option.Code))
                {
                    continue;
                }

                labelsByCode[option.Code] = option.Label;
            }
        }

        public static string Resolve(IReadOnlyDictionary<string, string> labelsByCode, string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return code;
            }

            if (labelsByCode != null && labelsByCode.TryGetValue(code, out var label) && !string.IsNullOrWhiteSpace(label))
            {
                return label;
            }

            return code;
        }
    }
}
