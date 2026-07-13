using Domain.Constants;
using Domain.Enums;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.DataSeeder
{
    public static class IdentitySeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("IdentitySeeder");

            await SeedRoleAsync(roleManager, RoleNames.SuperAdmin, "Full, unrestricted system access.");
            await SeedRoleAsync(roleManager, RoleNames.Admin, "Administrative access to manage users and content.");
            await SeedRoleAsync(roleManager, RoleNames.User, "Standard end-user access.");

            await SeedUserAsync(userManager, configuration, logger, "Seed:SuperAdmin", RoleNames.SuperAdmin, UserType.SuperAdmin);
            await SeedUserAsync(userManager, configuration, logger, "Seed:Admin", RoleNames.Admin, UserType.Admin);
            await SeedUserAsync(userManager, configuration, logger, "Seed:User", RoleNames.User, UserType.User);
        }

        private static async Task SeedRoleAsync(RoleManager<ApplicationRole> roleManager, string roleName, string description)
        {
            var roleExists = await roleManager.RoleExistsAsync(roleName);
            if (roleExists)
            {
                return;
            }

            var role = new ApplicationRole
            {
                Name = roleName,
                Description = description
            };

            await roleManager.CreateAsync(role);
        }

        private static async Task SeedUserAsync(
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration,
            ILogger logger,
            string configSection,
            string roleName,
            UserType userType)
        {
            var userName = configuration[configSection + ":UserName"];
            var email = configuration[configSection + ":Email"];
            var password = configuration[configSection + ":Password"];
            var firstName = configuration[configSection + ":FirstName"];
            var lastName = configuration[configSection + ":LastName"];

            var existingUser = await userManager.FindByNameAsync(userName);
            if (existingUser != null)
            {
                return;
            }

            var user = new ApplicationUser
            {
                UserName = userName,
                Email = email,
                EmailConfirmed = true,
                FirstName = firstName,
                LastName = lastName,
                Gender = Gender.Other,
                UserType = userType,
                IsActive = true,
                IsTosAgreed = true,
                LastPasswordChangedTs = DateTimeOffset.UtcNow
            };

            var createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                var errorMessages = new List<string>();
                foreach (var error in createResult.Errors)
                {
                    errorMessages.Add(error.Description);
                }

                var combinedMessage = string.Join(" ", errorMessages);
                logger.LogWarning("Failed to seed user '{UserName}': {Errors}", userName, combinedMessage);
                return;
            }

            await userManager.AddToRoleAsync(user, roleName);
        }
    }
}
