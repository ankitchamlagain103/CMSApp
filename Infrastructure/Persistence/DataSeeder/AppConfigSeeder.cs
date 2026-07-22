using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Persistence.DataSeeder
{
    // Seeds the baseline UI-configuration rows (see Docs/app_config_implementation_guide.md for
    // the full param catalog and what the frontend does with each). Idempotent by ConfigParam and
    // strictly create-if-missing: an existing row is NEVER updated, so values an admin has edited
    // through the API survive every restart. Values here are safe defaults, not secrets -- every
    // enabled row is world-readable via GET /api/appconfigs/public.
    public static class AppConfigSeeder
    {
        private const string GroupGeneral = "GENERAL";
        private const string GroupTheme = "THEME";
        private const string GroupAnnouncement = "ANNOUNCEMENT";

        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var baselineConfigs = BuildBaselineConfigs();

            var existingParams = await dbContext.AppConfigs
                .Select(appConfig => appConfig.ConfigParam)
                .ToListAsync();

            var newRowsAdded = false;
            foreach (var baselineConfig in baselineConfigs)
            {
                if (existingParams.Contains(baselineConfig.ConfigParam))
                {
                    continue;
                }

                dbContext.AppConfigs.Add(baselineConfig);
                newRowsAdded = true;
            }

            if (newRowsAdded)
            {
                await dbContext.SaveChangesAsync();
            }
        }

        private static List<AppConfig> BuildBaselineConfigs()
        {
            var baselineConfigs = new List<AppConfig>
            {
                // GENERAL -- identity & branding
                BuildConfig("APP_NAME", "CMSApp", GroupGeneral),
                BuildConfig("APP_TAGLINE", "Content Management System", GroupGeneral),
                BuildConfig("FOOTER_TEXT", "© 2026 CMSApp", GroupGeneral),
                BuildConfig("SUPPORT_EMAIL", "support@cmsapp.local", GroupGeneral),

                // School identity for printed documents (2026-07-17) -- the payment-receipt
                // template's header. APP_NAME already doubles as the school's display name; set
                // these two once via PUT /api/appconfigs/{id} before printing real receipts.
                BuildConfig("SCHOOL_ADDRESS", "", GroupGeneral),
                BuildConfig("SCHOOL_PHONE", "", GroupGeneral),

                // Fee generation (2026-07-16): day of the billing month a generated fee
                // invoice's DueDate defaults to (clamped to the month's length; editable per
                // Draft invoice before finalization).
                BuildConfig("FEE_DUE_DAY_OF_MONTH", "10", GroupGeneral),

                // THEME -- appearance; color values are applied by the frontend as CSS variables
                BuildConfig("THEME_MODE", "SYSTEM", GroupTheme),
                BuildConfig("PRIMARY_COLOR", "#4F46E5", GroupTheme),
                BuildConfig("SECONDARY_COLOR", "#EC4899", GroupTheme),
                BuildConfig("ACCENT_COLOR", "#0EA5E9", GroupTheme),
                BuildConfig("SUCCESS_COLOR", "#16A34A", GroupTheme),
                BuildConfig("WARNING_COLOR", "#D97706", GroupTheme),
                BuildConfig("ERROR_COLOR", "#DC2626", GroupTheme),
                BuildConfig("FONT_FAMILY", "Inter", GroupTheme),
                BuildConfig("BORDER_RADIUS", "8px", GroupTheme),

                // ANNOUNCEMENT -- app-wide banner, toggled by value (the rows stay enabled so the
                // public endpoint always returns the toggle itself)
                BuildConfig("ANNOUNCEMENT_ENABLED", "false", GroupAnnouncement),
                BuildConfig("ANNOUNCEMENT_TEXT", "Welcome to CMSApp", GroupAnnouncement),
                BuildConfig("ANNOUNCEMENT_TYPE", "INFO", GroupAnnouncement),
                BuildConfig("MAINTENANCE_MODE", "false", GroupAnnouncement)
            };

            return baselineConfigs;
        }

        private static AppConfig BuildConfig(string configParam, string configValue, string configGroup)
        {
            var appConfig = new AppConfig
            {
                ConfigParam = configParam,
                ConfigValue = configValue,
                ConfigGroup = configGroup,
                IsEnable = true
            };

            return appConfig;
        }
    }
}
