using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Identity.Common
{
    // Shared by AuthService.GoogleLoginAsync and IdentityService.ProvisionPortalAccountAsync --
    // both need a username for an account created with no admin-supplied one. Derives it from the
    // email's local part, de-duplicated with a numeric suffix.
    public static class UserNameGenerator
    {
        public static async Task<string> GenerateUniqueAsync(UserManager<ApplicationUser> userManager, string email)
        {
            var baseUserName = email.Split('@')[0];
            var candidateUserName = baseUserName;
            var suffix = 1;

            while (await userManager.FindByNameAsync(candidateUserName) != null)
            {
                candidateUserName = baseUserName + suffix;
                suffix++;
            }

            return candidateUserName;
        }
    }
}
