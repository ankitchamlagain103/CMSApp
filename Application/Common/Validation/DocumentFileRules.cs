namespace Application.Common.Validation
{
    // Upload rules for document files (teacher documents today; reuse for any future document
    // table). Kept as code constants rather than configuration -- they are a security boundary,
    // not a per-environment preference.
    public static class DocumentFileRules
    {
        public const long MaxFileSizeBytes = 10 * 1024 * 1024;

        private static readonly string[] AllowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };

        public static bool IsAllowedExtension(string fileName)
        {
            var extension = Path.GetExtension(fileName);
            if (string.IsNullOrWhiteSpace(extension))
            {
                return false;
            }

            var normalizedExtension = extension.ToLowerInvariant();
            foreach (var allowedExtension in AllowedExtensions)
            {
                if (normalizedExtension == allowedExtension)
                {
                    return true;
                }
            }

            return false;
        }

        public static string AllowedExtensionsDisplay()
        {
            var display = string.Join(", ", AllowedExtensions);
            return display;
        }
    }
}
