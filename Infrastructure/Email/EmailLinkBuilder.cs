using Microsoft.Extensions.Configuration;

namespace Infrastructure.Email
{
    public static class EmailLinkBuilder
    {
        public static string BuildVerifyEmailLink(IConfiguration configuration, Guid userId, string token)
        {
            var encodedToken = Uri.EscapeDataString(token);
            var clientBaseUrl = configuration["App:ClientBaseUrl"];
            if (string.IsNullOrWhiteSpace(clientBaseUrl))
            {
                return "userId=" + userId + "&token=" + encodedToken;
            }

            var link = clientBaseUrl.TrimEnd('/') + "/verify-email?userId=" + userId + "&token=" + encodedToken;
            return link;
        }

        public static string BuildResetPasswordLink(IConfiguration configuration, Guid userId, string token)
        {
            var encodedToken = Uri.EscapeDataString(token);
            var clientBaseUrl = configuration["App:ClientBaseUrl"];
            if (string.IsNullOrWhiteSpace(clientBaseUrl))
            {
                return "userId=" + userId + "&token=" + encodedToken;
            }

            var link = clientBaseUrl.TrimEnd('/') + "/reset-password?userId=" + userId + "&token=" + encodedToken;
            return link;
        }
    }
}
