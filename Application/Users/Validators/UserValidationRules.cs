using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace Application.Users.Validators
{
    public static class UserValidationRules
    {
        public static readonly Regex NoHtmlPattern = new Regex("[<>]");
        public static readonly Regex PhoneE164Pattern = new Regex(@"^\+[1-9]\d{1,14}$");
        public static readonly Regex CountryIso3Pattern = new Regex("^[A-Z]{3}$");

        public static bool BeAValidAge(DateTime? dob)
        {
            var today = DateTime.UtcNow.Date;
            var birthDate = dob.Value.Date;
            var age = today.Year - birthDate.Year;
            if (birthDate > today.AddYears(-age))
            {
                age--;
            }

            return age >= 13 && age <= 120;
        }

        public static bool BeAValidIpList(string userIpAllowed)
        {
            var ipTokens = userIpAllowed.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (ipTokens.Length == 0)
            {
                return false;
            }

            foreach (var ipToken in ipTokens)
            {
                var trimmedToken = ipToken.Trim();
                if (!IPAddress.TryParse(trimmedToken, out var parsedAddress))
                {
                    return false;
                }

                // IPAddress.TryParse accepts shorthand IPv4 forms ("192.168.1" parses as
                // 192.168.0.1) -- require the full dotted-quad spelling so a typo can't
                // silently allow a different address than the admin intended.
                if (parsedAddress.AddressFamily == AddressFamily.InterNetwork && trimmedToken.Split('.').Length != 4)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
