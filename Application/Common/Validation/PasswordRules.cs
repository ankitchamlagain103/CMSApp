using System.Text.RegularExpressions;

namespace Application.Common.Validation
{
    public static class PasswordRules
    {
        public static readonly Regex UppercasePattern = new Regex("[A-Z]");
        public static readonly Regex LowercasePattern = new Regex("[a-z]");
        public static readonly Regex DigitPattern = new Regex("[0-9]");
        public static readonly Regex SpecialCharacterPattern = new Regex("[^a-zA-Z0-9]");

        public static readonly HashSet<string> CommonPasswords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "password", "password1", "password123", "12345678", "123456789", "qwerty123",
            "letmein", "welcome1", "admin123", "changeme", "iloveyou", "qwertyuiop"
        };
    }
}
