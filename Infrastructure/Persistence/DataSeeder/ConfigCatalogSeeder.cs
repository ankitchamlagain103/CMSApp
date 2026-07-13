using Domain.Constants;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Persistence.DataSeeder
{
    // Seeds the ConfigType catalogs the student-management feature validates against (TypeCodes
    // from Domain/Constants/ConfigTypeCodes -- keep the two in sync). Grade/Section/Subject get
    // the TYPE only: their options vary per school and are admin data (POST /api/configs).
    // GuardianRelationship and TeacherQualification also get default OPTION rows, since those
    // vocabularies are near-universal. Idempotent: types by TypeCode, options by (TypeCode, Code),
    // create-if-missing only.
    public static class ConfigCatalogSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            await EnsureConfigTypeAsync(dbContext, ConfigTypeCodes.Grade, "Grade", "School grade/level catalog (student management)");
            await EnsureConfigTypeAsync(dbContext, ConfigTypeCodes.Section, "Section", "Class section catalog (student management)");
            await EnsureConfigTypeAsync(dbContext, ConfigTypeCodes.Subject, "Subject", "Subject catalog (student management); AdditionalValue1 = short name, AdditionalValue2 = credit, AdditionalValue3 = category");
            await EnsureConfigTypeAsync(dbContext, ConfigTypeCodes.GuardianRelationship, "Guardian Relationship", "Student-guardian relationship catalog");
            await EnsureConfigTypeAsync(dbContext, ConfigTypeCodes.TeacherQualification, "Teacher Qualification", "Teacher qualification level catalog");
            await EnsureConfigTypeAsync(dbContext, ConfigTypeCodes.DocumentType, "Document Type", "Identity/verification document catalog (teacher documents); AdditionalValue1 = 'Y' when the document typically has an expiry (license/report)");

            await EnsureConfigAsync(dbContext, ConfigTypeCodes.GuardianRelationship, "FATHER", "Father", 1);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.GuardianRelationship, "MOTHER", "Mother", 2);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.GuardianRelationship, "GRANDFATHER", "Grandfather", 3);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.GuardianRelationship, "GRANDMOTHER", "Grandmother", 4);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.GuardianRelationship, "BROTHER", "Brother", 5);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.GuardianRelationship, "SISTER", "Sister", 6);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.GuardianRelationship, "UNCLE", "Uncle", 7);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.GuardianRelationship, "AUNT", "Aunt", 8);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.GuardianRelationship, "LEGAL_GUARDIAN", "Legal Guardian", 9);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.GuardianRelationship, "OTHER", "Other", 10);

            await EnsureConfigAsync(dbContext, ConfigTypeCodes.TeacherQualification, "PHD", "Doctorate (PhD)", 1);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.TeacherQualification, "MASTERS", "Master's Degree", 2);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.TeacherQualification, "BACHELORS", "Bachelor's Degree", 3);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.TeacherQualification, "DIPLOMA", "Diploma", 4);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.TeacherQualification, "CERTIFICATE", "Certificate", 5);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.TeacherQualification, "OTHER", "Other", 6);

            await EnsureConfigAsync(dbContext, ConfigTypeCodes.DocumentType, "CITIZENSHIP", "Citizenship Certificate", 1);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.DocumentType, "ID_CARD", "ID Card", 2);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.DocumentType, "PAN_CARD", "PAN Card", 3);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.DocumentType, "PASSPORT", "Passport", 4);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.DocumentType, "DRIVING_LICENSE", "Driving License", 5);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.DocumentType, "POLICE_REPORT", "Police Report", 6);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.DocumentType, "ACADEMIC_CERTIFICATE", "Academic Certificate", 7);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.DocumentType, "APPOINTMENT_LETTER", "Appointment Letter", 8);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.DocumentType, "OTHER", "Other", 9);
        }

        private static async Task EnsureConfigTypeAsync(ApplicationDbContext dbContext, int typeCode, string name, string description)
        {
            var typeExists = await dbContext.ConfigTypes.AnyAsync(configType => configType.TypeCode == typeCode);
            if (typeExists)
            {
                return;
            }

            var configType = new ConfigType
            {
                TypeCode = typeCode,
                Name = name,
                Description = description
            };

            dbContext.ConfigTypes.Add(configType);
            await dbContext.SaveChangesAsync();
        }

        private static async Task EnsureConfigAsync(ApplicationDbContext dbContext, int typeCode, string code, string label, int order)
        {
            var configExists = await dbContext.Configs.AnyAsync(config => config.TypeCode == typeCode && config.Code == code);
            if (configExists)
            {
                return;
            }

            var config = new Config
            {
                TypeCode = typeCode,
                Code = code,
                Label = label,
                Order = order
            };

            dbContext.Configs.Add(config);
            await dbContext.SaveChangesAsync();
        }
    }
}
