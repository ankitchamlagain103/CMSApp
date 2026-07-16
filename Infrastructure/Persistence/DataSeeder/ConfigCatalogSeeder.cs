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
            await EnsureConfigTypeAsync(dbContext, ConfigTypeCodes.StudentDocumentType, "Student Document Type", "Admission/record document catalog (student documents)");
            await EnsureConfigTypeAsync(dbContext, ConfigTypeCodes.DiscountType, "Discount Type", "Fee discount reason catalog (fee management)");
            await EnsureConfigTypeAsync(dbContext, ConfigTypeCodes.ScholarshipType, "Scholarship Type", "Scholarship criteria catalog (fee management) -- the configurable 'topper/exam/social category/...' criteria; admin-extensible via POST /api/configs");
            await EnsureConfigTypeAsync(dbContext, ConfigTypeCodes.FeeCategory, "Fee Category", "Permitted school fee categories (fee management) -- Tuition/Annual/Admission/Deposit/Examination/Computer/SpecialTraining/Hostel/Meal/Transportation/EducationalTour");
            await EnsureConfigTypeAsync(dbContext, ConfigTypeCodes.EmployeeCategory, "Employee Category", "Staff department/category catalog (employee management)");
            await EnsureConfigTypeAsync(dbContext, ConfigTypeCodes.JobPosition, "Job Position", "Staff job position catalog (employee management) -- Teacher/Principal/Vice Principal are the only positions eligible for a Teacher profile (see Domain/Constants/JobPositionCodes)");
            await EnsureConfigTypeAsync(dbContext, ConfigTypeCodes.SalaryComponentType, "Salary Component Type", "Compensation-plan income line items (payroll) -- 'BASIC' (Domain/Constants/SalaryComponentCodes) is the well-known code Percentage-valued components/deductions resolve their rate against");
            await EnsureConfigTypeAsync(dbContext, ConfigTypeCodes.DeductionType, "Deduction Type", "Compensation-plan deduction/loan/advance line items (payroll)");
            await EnsureConfigTypeAsync(dbContext, ConfigTypeCodes.InsuranceType, "Insurance Type", "Life/Health/Housing insurance catalog (payroll); AdditionalValue1 = that type's Nepal tax-deduction cap amount");

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

            await EnsureConfigAsync(dbContext, ConfigTypeCodes.StudentDocumentType, "BIRTH_CERTIFICATE", "Birth Certificate", 1);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.StudentDocumentType, "TRANSFER_CERTIFICATE", "Transfer Certificate (TC)", 2);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.StudentDocumentType, "CHARACTER_CERTIFICATE", "Character Certificate", 3);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.StudentDocumentType, "PREVIOUS_MARKSHEET", "Previous Marksheet / Report Card", 4);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.StudentDocumentType, "CITIZENSHIP", "Citizenship Certificate", 5);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.StudentDocumentType, "PASSPORT", "Passport", 6);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.StudentDocumentType, "PHOTO", "Passport-size Photo", 7);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.StudentDocumentType, "IMMUNIZATION_RECORD", "Immunization / Health Record", 8);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.StudentDocumentType, "DISABILITY_CARD", "Disability Card", 9);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.StudentDocumentType, "MIGRATION_CERTIFICATE", "Migration Certificate", 10);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.StudentDocumentType, "GUARDIAN_CITIZENSHIP", "Guardian's Citizenship Copy", 11);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.StudentDocumentType, "OTHER", "Other", 12);

            // AdditionalValue1/2 = the "global" default rate (ValueType name / Value) this
            // discount/scholarship type applies unless a caller overrides it individually --
            // see EnrollmentService.ResolveDefaultAward. Left blank (no default) for types that
            // are always individually assessed (financial hardship, catch-all "other").
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.DiscountType, "SIBLING", "Sibling Discount", 1, "Percentage", "10");
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.DiscountType, "STAFF_CHILD", "Staff Child Discount", 2, "Percentage", "50");
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.DiscountType, "EARLY_PAYMENT", "Early Payment Discount", 3, "Percentage", "5");
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.DiscountType, "FINANCIAL_HARDSHIP", "Financial Hardship Discount", 4);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.DiscountType, "OTHER", "Other", 5);

            await EnsureConfigAsync(dbContext, ConfigTypeCodes.ScholarshipType, "CLASS_TOPPER", "Class Topper Scholarship", 1, "Percentage", "100");
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.ScholarshipType, "MERIT_EXAM", "Merit (Exam Performance) Scholarship", 2, "Percentage", "25");
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.ScholarshipType, "SOCIAL_CATEGORY", "Social Category Scholarship", 3, "Percentage", "20");
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.ScholarshipType, "SPORTS", "Sports Scholarship", 4, "Percentage", "15");
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.ScholarshipType, "OTHER", "Other", 5);

            // The 11 "Permitted Fee Categories" -- type-only-vs-defaulted follows the same split as
            // Grade/Section (school-specific amounts, admin-created via POST /api/feestructures)
            // vs GuardianRelationship (near-universal vocabulary): the categories themselves are
            // the fixed, regulation-driven part, so all 11 are seeded; per-class amounts are not.
            // AdditionalValue1/2/3 (default FrequencyType name / IsOptional / IsRefundable) are the
            // "global config" defaults a UI can prefill from when adding a class fee item -- the
            // resulting FeeStructureItem is a fully independent named row with no ongoing link back
            // to this code (2026-07-15 header+items redesign, see
            // Docs/fee_management_implementation_guide.md).
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.FeeCategory, "TUITION", "Monthly Tuition Fee", 1, "Monthly", "false", "false");
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.FeeCategory, "ANNUAL", "Annual Fee", 2, "Annual", "false", "false");
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.FeeCategory, "ADMISSION", "Admission Fee", 3, "OneTime", "false", "false");
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.FeeCategory, "DEPOSIT", "Deposit (Refundable)", 4, "OneTime", "false", "true");
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.FeeCategory, "EXAMINATION", "Examination Fee", 5, "Annual", "false", "false");
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.FeeCategory, "COMPUTER", "Computer Fee", 6, "Monthly", "false", "false");
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.FeeCategory, "SPECIAL_TRAINING", "Special Training Fee", 7, "Monthly", "true", "false");
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.FeeCategory, "HOSTEL", "Hostel Fee", 8, "Monthly", "true", "false");
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.FeeCategory, "MEAL", "Meal Fee", 9, "Monthly", "true", "false");
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.FeeCategory, "TRANSPORTATION", "Transportation Fee", 10, "Monthly", "true", "false");
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.FeeCategory, "EDUCATIONAL_TOUR", "Educational Tour Fee", 11, "OneTime", "true", "false");

            // All seeded (fixed org taxonomy, same reasoning as FeeCategory) -- codes match
            // Domain/Constants/EmployeeCategoryCodes/JobPositionCodes exactly where those
            // constants exist.
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.EmployeeCategory, "ACADEMIC", "Academic", 1);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.EmployeeCategory, "ADMINISTRATION", "Administration", 2);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.EmployeeCategory, "FINANCE", "Finance", 3);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.EmployeeCategory, "LIBRARY", "Library", 4);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.EmployeeCategory, "TRANSPORT", "Transport", 5);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.EmployeeCategory, "IT", "IT", 6);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.EmployeeCategory, "HR", "HR", 7);

            await EnsureConfigAsync(dbContext, ConfigTypeCodes.JobPosition, "TEACHER", "Teacher", 1);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.JobPosition, "PRINCIPAL", "Principal", 2);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.JobPosition, "VICE_PRINCIPAL", "Vice Principal", 3);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.JobPosition, "ACCOUNTANT", "Accountant", 4);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.JobPosition, "RECEPTIONIST", "Receptionist", 5);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.JobPosition, "LIBRARIAN", "Librarian", 6);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.JobPosition, "IT_OFFICER", "IT Officer", 7);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.JobPosition, "DRIVER", "Driver", 8);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.JobPosition, "SECURITY_GUARD", "Security Guard", 9);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.JobPosition, "OFFICE_ASSISTANT", "Office Assistant", 10);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.JobPosition, "CLEANER", "Cleaner", 11);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.JobPosition, "OFFICE_HELP", "Office Help", 12);

            // "BASIC" (Domain/Constants/SalaryComponentCodes.Basic) must exist as a component
            // code option here for the "resolve percentages against Basic Salary" convention to
            // make sense to an admin building a compensation plan.
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.SalaryComponentType, "BASIC", "Basic Salary", 1);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.SalaryComponentType, "SSF_CONTRIBUTION", "SSF Contribution", 2);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.SalaryComponentType, "COMMUNICATION_ALLOWANCE", "Communication Allowance", 3);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.SalaryComponentType, "DEARNESS_ALLOWANCE", "Dearness Allowance", 4);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.SalaryComponentType, "TRAVEL_ALLOWANCE", "Travel Allowance", 5);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.SalaryComponentType, "OTHER_ALLOWANCE", "Other Allowance", 6);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.SalaryComponentType, "FESTIVAL_BONUS", "Festival (Dashain) Bonus", 7);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.SalaryComponentType, "LEAVE_ENCASHMENT", "Leave Encashment", 8);

            await EnsureConfigAsync(dbContext, ConfigTypeCodes.DeductionType, "SSF_DEDUCTION", "SSF Deduction", 1);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.DeductionType, "LOAN", "Loan", 2);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.DeductionType, "ADVANCE", "Advance", 3);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.DeductionType, "OTHER", "Other", 4);

            // AdditionalValue1 = that insurance type's Nepal tax-deduction cap (illustrative --
            // verify against the current Income Tax Act figures before relying on it, same
            // caution as the seeded tax slabs in PayrollSeeder).
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.InsuranceType, "LIFE", "Life Insurance", 1, additionalValue1: "40000");
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.InsuranceType, "HEALTH", "Health Insurance", 2, additionalValue1: "20000");
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.InsuranceType, "HOUSING", "Housing Insurance", 3, additionalValue1: "25000");
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

        private static async Task EnsureConfigAsync(ApplicationDbContext dbContext, int typeCode, string code, string label, int order, string additionalValue1 = null, string additionalValue2 = null, string additionalValue3 = null)
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
                Order = order,
                AdditionalValue1 = additionalValue1,
                AdditionalValue2 = additionalValue2,
                AdditionalValue3 = additionalValue3
            };

            dbContext.Configs.Add(config);
            await dbContext.SaveChangesAsync();
        }
    }
}
