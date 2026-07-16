namespace Application.Common.Helpers
{
    // Builds the next value of a prefixed sequence number (ADM2026001, EMP2026001, ...): scans
    // the existing numbers sharing the prefix, takes the highest numeric suffix, and formats
    // max+1 zero-padded to minWidth (growing naturally past the padding, e.g. ...999 -> ...1000).
    // Suffixes are parsed numerically, so ordering never depends on string comparison.
    public static class NumberSequenceHelper
    {
        public static string Next(string prefix, IReadOnlyList<string> existingNumbers, int minWidth)
        {
            var highestSuffix = 0;
            foreach (var existingNumber in existingNumbers)
            {
                if (existingNumber == null || !existingNumber.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var suffixText = existingNumber.Substring(prefix.Length);
                if (int.TryParse(suffixText, out var suffixValue) && suffixValue > highestSuffix)
                {
                    highestSuffix = suffixValue;
                }
            }

            var nextValue = highestSuffix + 1;
            var nextNumber = prefix + nextValue.ToString("D" + minWidth);
            return nextNumber;
        }
    }
}
