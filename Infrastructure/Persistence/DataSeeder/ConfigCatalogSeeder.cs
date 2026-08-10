using Domain.Constants;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Persistence.DataSeeder
{
    // Seeds the ConfigType catalogs the student-management feature validates against (TypeCodes
    // from Domain/Constants/ConfigTypeCodes -- keep the two in sync). Grade/Section/Subject get
    // the TYPE only: their options vary per school and are admin data (POST /api/configs).
    // GuardianRelationship and EmployeeQualification also get default OPTION rows, since those
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
            // AdditionalValue2 = GRADE_CODE (2026-08-03): "ALL" when the subject is offered to
            // every grade, or a comma-separated Domain/Constants GradeCodes list when it's only
            // offered to certain grades -- e.g. "NINE,TEN,ELEVEN,TWELVE" for Computer Science.
            // Informational for the admin catalog screen; ClassSubject assignment itself is
            // unaffected (AcademicClassService.AssignSubjectAsync still validates SubjectCode
            // against this catalog by Code alone, it doesn't cross-check GRADE_CODE against the
            // class being assigned to). SampleDataSeeder computes this value from the same
            // grade->subject mapping it uses to actually seed ClassSubject rows, so the two can't
            // drift; a subject created via POST /api/configs leaves it blank until set manually.
            await EnsureConfigTypeAsync(dbContext, ConfigTypeCodes.Subject, "Subject", "Subject catalog (student management); AdditionalValue1 = short name, AdditionalValue2 = GRADE_CODE (\"ALL\" or comma-separated grade codes the subject applies to), AdditionalValue3 = category");
            await EnsureConfigTypeAsync(dbContext, ConfigTypeCodes.GuardianRelationship, "Guardian Relationship", "Student-guardian relationship catalog");
            await EnsureConfigTypeAsync(dbContext, ConfigTypeCodes.EmployeeQualification, "Employee Qualification", "Employee qualification level catalog (2026-07-23: renamed from 'Teacher Qualification' -- generic to every staff member, not teaching-specific)");
            await EnsureConfigTypeAsync(dbContext, ConfigTypeCodes.DocumentType, "Document Type", "Identity/verification document catalog (employee documents); AdditionalValue1 = 'Y' when the document typically has an expiry (license/report)");
            await EnsureConfigTypeAsync(dbContext, ConfigTypeCodes.StudentDocumentType, "Student Document Type", "Admission/record document catalog (student documents)");
            await EnsureConfigTypeAsync(dbContext, ConfigTypeCodes.DiscountType, "Discount Type", "Fee discount reason catalog (fee management)");
            await EnsureConfigTypeAsync(dbContext, ConfigTypeCodes.ScholarshipType, "Scholarship Type", "Scholarship criteria catalog (fee management) -- the configurable 'topper/exam/social category/...' criteria; admin-extensible via POST /api/configs");
            await EnsureConfigTypeAsync(dbContext, ConfigTypeCodes.FeeCategory, "Fee Category", "Permitted school fee categories (fee management) -- Tuition/Annual/Admission/Deposit/Examination/Computer/SpecialTraining/Hostel/Meal/Transportation/EducationalTour");
            await EnsureConfigTypeAsync(dbContext, ConfigTypeCodes.EmployeeCategory, "Employee Category", "Staff department/category catalog (employee management)");
            await EnsureConfigTypeAsync(dbContext, ConfigTypeCodes.JobPosition, "Job Position", "Staff job position catalog (employee management) -- Teacher/Principal/Vice Principal are the positions EmployeeRoleHelper.IsTeachingStaff treats as teaching staff (see Domain/Constants/JobPositionCodes)");
            await EnsureConfigTypeAsync(dbContext, ConfigTypeCodes.SalaryComponentType, "Salary Component Type", "Compensation-plan income line items (payroll) -- 'BASIC' (Domain/Constants/SalaryComponentCodes) is the well-known code Percentage-valued components/deductions resolve their rate against by default; AdditionalValue1 is the composite \"CALCULATE_TYPE|TYPE|FREQUENCY\" rule (Domain/Constants/SalaryLineCalculationModes), e.g. \"ADDITION|PERCENTAGE|MONTHLY\" for SSF_CONTRIBUTION at 20% (AdditionalValue2) of BASIC (AdditionalValue3) -- blank/unparseable keeps free-form per-line entry");
            await EnsureConfigTypeAsync(dbContext, ConfigTypeCodes.DeductionType, "Deduction Type", "Compensation-plan deduction/loan/advance line items (payroll) -- same composite AdditionalValue1 convention as Salary Component Type (SSF_DEDUCTION is locked to \"DEDUCTION|PERCENTAGE|MONTHLY\", 11% of BASIC)");
            await EnsureConfigTypeAsync(dbContext, ConfigTypeCodes.InsuranceType, "Insurance Type", "Life/Health/Housing insurance + Children's Education catalog (payroll); AdditionalValue1 = that type's Nepal tax-deduction cap amount, AdditionalValue2 = percentage of the actual annual amount that's eligible before the cap applies (blank/100 = the full amount, as for a straight insurance premium)");
            await EnsureConfigTypeAsync(dbContext, ConfigTypeCodes.SalaryAdjustmentType, "Salary Adjustment Type", "Pre-run monthly payroll override catalog (payroll runs) -- UNPAID_LEAVE gets special day-count handling (Domain/Constants/SalaryAdjustmentTypeCodes); AdditionalValue1 is the same composite \"CALCULATE_TYPE|TYPE|FREQUENCY\" rule as Salary Component/Deduction Type (2026-07-23, replacing the old bare EARNING/DEDUCTION value) -- CALCULATE_TYPE is enforced against the adjustment's own Direction (ADDITION requires Increase, DEDUCTION requires Decrease); TYPE/FREQUENCY are metadata only (SalaryAdjustment has no catalog-enforced percentage lock or FrequencyType field) -- blank (e.g. OTHER) leaves Direction free");
            await EnsureConfigTypeAsync(dbContext, ConfigTypeCodes.FeeAdjustmentType, "Fee Adjustment Type", "Pre-generation monthly fee override catalog (fee invoices); AdditionalValue1 = suggested direction (CHARGE/CREDIT) a UI can prefill from");
            await EnsureConfigTypeAsync(dbContext, ConfigTypeCodes.SsfRate, "SSF Rate", "Social Security Fund contribution rates (payroll) -- EMPLOYEE_SHARE/EMPLOYER_SHARE (Domain/Constants/SsfShareCodes); AdditionalValue1 = that share's percentage of Basic Salary, admin-editable when the law changes");

            // Employee "org" fields (2026-07-23). Branch gets the TYPE only -- school-specific,
            // admin-created via POST /api/configs, same split as Grade/Section. Province and
            // Level get default OPTION rows since those vocabularies are near-universal (Nepal's
            // 7 federal provinces; a generic seniority ladder).
            await EnsureConfigTypeAsync(dbContext, ConfigTypeCodes.Branch, "Branch", "Employee branch/office catalog (employee management)");
            await EnsureConfigTypeAsync(dbContext, ConfigTypeCodes.Province, "Province", "Employee province catalog (employee management) -- also usable to scope a PublicHoliday CalendarEvent");
            await EnsureConfigTypeAsync(dbContext, ConfigTypeCodes.EmployeeLevel, "Employee Level", "Employee seniority level catalog (employee management)");

            // Address chain (2026-07-24), extending Province into a full Nepal address.
            // AdditionalValue1 = parent code (District's own ProvinceCode; LocalLevel's own
            // DistrictCode); LocalLevel's AdditionalValue2 also denormalizes its ProvinceCode so a
            // UI can reverse-map both levels from one LocalLevel option in a single lookup.
            // AdditionalValue3 on LocalLevel is its type (Domain/Constants/LocalLevelTypeCodes).
            await EnsureConfigTypeAsync(dbContext, ConfigTypeCodes.District, "District", "Nepal district catalog (employee address); AdditionalValue1 = its ProvinceCode");
            await EnsureConfigTypeAsync(dbContext, ConfigTypeCodes.LocalLevel, "Local Level", "Nepal local-level (municipality/rural municipality/metropolitan/sub-metropolitan city) catalog (employee address); AdditionalValue1 = its DistrictCode, AdditionalValue2 = its ProvinceCode, AdditionalValue3 = its type (Domain/Constants/LocalLevelTypeCodes)");

            // Class period timing lived here briefly (2026-07-30..2026-08-03, "Exam Period" then
            // "Class Period") and was removed the same day it was renamed -- moved to the real
            // Domain/Entities/TimePeriod + ClassTimePeriod tables instead, since "certain classes
            // run different period structures" is a relationship a flat Config option list can't
            // express. See TimePeriodSeeder and Docs/time_period_and_class_routine_implementation_guide.md.

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

            await EnsureConfigAsync(dbContext, ConfigTypeCodes.EmployeeQualification, "PHD", "Doctorate (PhD)", 1);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.EmployeeQualification, "MASTERS", "Master's Degree", 2);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.EmployeeQualification, "BACHELORS", "Bachelor's Degree", 3);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.EmployeeQualification, "DIPLOMA", "Diploma", 4);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.EmployeeQualification, "CERTIFICATE", "Certificate", 5);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.EmployeeQualification, "OTHER", "Other", 6);

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
            // AdditionalValue1 is the category's fee_frequency (Domain/Constants/FeeFrequencyCodes,
            // normative as of 2026-07-16 -- it drives the fee-generation default and is validated
            // on Config create/update for this type); AdditionalValue2/3 (IsOptional/IsRefundable)
            // stay UI-prefill defaults. The resulting FeeStructureItem is a fully independent row
            // whose own FrequencyType is what generation executes.
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.FeeCategory, "TUITION", "Monthly Tuition Fee", 1, FeeFrequencyCodes.Monthly, "false", "false");
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.FeeCategory, "ANNUAL", "Annual Fee", 2, FeeFrequencyCodes.Annual, "false", "false");
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.FeeCategory, "ADMISSION", "Admission Fee", 3, FeeFrequencyCodes.OneTime, "false", "false");
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.FeeCategory, "DEPOSIT", "Deposit (Refundable)", 4, FeeFrequencyCodes.OneTime, "false", "true");
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.FeeCategory, "EXAMINATION", "Examination Fee", 5, FeeFrequencyCodes.Annual, "false", "false");
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.FeeCategory, "COMPUTER", "Computer Fee", 6, FeeFrequencyCodes.Monthly, "false", "false");
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.FeeCategory, "SPECIAL_TRAINING", "Special Training Fee", 7, FeeFrequencyCodes.Monthly, "true", "false");
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.FeeCategory, "HOSTEL", "Hostel Fee", 8, FeeFrequencyCodes.Monthly, "true", "false");
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.FeeCategory, "MEAL", "Meal Fee", 9, FeeFrequencyCodes.Monthly, "true", "false");
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.FeeCategory, "TRANSPORTATION", "Transportation Fee", 10, FeeFrequencyCodes.Monthly, "true", "false");
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.FeeCategory, "EDUCATIONAL_TOUR", "Educational Tour Fee", 11, FeeFrequencyCodes.OneTime, "true", "false");

            // One-time targeted normalization (2026-07-16): fee_frequency became normative, so
            // FeeCategory rows whose AdditionalValue1 is blank or a legacy enum name
            // ("Monthly"/"Annual"/"OneTime", seeded before the codes existed) are rewritten to
            // the canonical MONTHLY/ANNUAL/ONE_TIME codes. An already-canonical value -- admin-set
            // or otherwise -- is never touched.
            await NormalizeFeeCategoryFrequenciesAsync(dbContext);

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
            // make sense to an admin building a compensation plan. AdditionalValue1 is the
            // composite "CALCULATE_TYPE|TYPE|FREQUENCY" rule (2026-07-23, Domain/Constants/
            // SalaryLineCalculationModes) -- every SalaryComponentType row below carries it now,
            // not just the percentage-locked ones.
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.SalaryComponentType, "BASIC", "Basic Salary", 1, additionalValue1: "ADDITION|FIXED|MONTHLY");
            // Percentage-locked (2026-07-22, format updated 2026-07-23): AdditionalValue2 = the
            // statutory employer SSF rate, AdditionalValue3 = the base component's code -- see
            // SalaryLineCalculationHelper. Locks out exactly the mistake that prompted this
            // ("SSF_CONTRIBUTION"/"SSF_DEDUCTION" hand-entered at the wrong percentage, e.g. 31%
            // combining both shares into one line).
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.SalaryComponentType, "SSF_CONTRIBUTION", "SSF Contribution", 2, additionalValue1: "ADDITION|PERCENTAGE|MONTHLY", additionalValue2: "20", additionalValue3: "BASIC");
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.SalaryComponentType, "COMMUNICATION_ALLOWANCE", "Communication Allowance", 3, additionalValue1: "ADDITION|FIXED|MONTHLY");
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.SalaryComponentType, "DEARNESS_ALLOWANCE", "Dearness Allowance", 4, additionalValue1: "ADDITION|FIXED|MONTHLY");
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.SalaryComponentType, "TRAVEL_ALLOWANCE", "Travel Allowance", 5, additionalValue1: "ADDITION|FIXED|MONTHLY");
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.SalaryComponentType, "OTHER_ALLOWANCE", "Other Allowance", 6, additionalValue1: "ADDITION|FIXED|MONTHLY");
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.SalaryComponentType, "FESTIVAL_BONUS", "Festival (Dashain) Bonus", 7, additionalValue1: "ADDITION|FIXED|ONE_TIME");
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.SalaryComponentType, "LEAVE_ENCASHMENT", "Leave Encashment", 8, additionalValue1: "ADDITION|FIXED|ONE_TIME");
            // 2026-07-21: rounds out the "Common Salary Components" earnings list requested for
            // the Compensation Plan form (House Rent/Medical/Overtime/Bonus were missing options,
            // forcing admins to mistype them under the catch-all OTHER_ALLOWANCE code).
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.SalaryComponentType, "HOUSE_RENT_ALLOWANCE", "House Rent Allowance", 9, additionalValue1: "ADDITION|FIXED|MONTHLY");
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.SalaryComponentType, "MEDICAL_ALLOWANCE", "Medical Allowance", 10, additionalValue1: "ADDITION|FIXED|MONTHLY");
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.SalaryComponentType, "OVERTIME", "Overtime", 11, additionalValue1: "ADDITION|FIXED|MONTHLY");
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.SalaryComponentType, "BONUS", "Bonus", 12, additionalValue1: "ADDITION|FIXED|ONE_TIME");

            // Percentage-locked, same convention as SSF_CONTRIBUTION above -- the employee's own
            // 11% share, not the combined 31% (employee 11% + employer 20%) that was mistakenly
            // hand-entered here on a real salary revision. Every DeductionType row below carries
            // the composite AdditionalValue1 rule now (2026-07-23), not just this one.
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.DeductionType, "SSF_DEDUCTION", "SSF Deduction", 1, additionalValue1: "DEDUCTION|PERCENTAGE|MONTHLY", additionalValue2: "11", additionalValue3: "BASIC");
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.DeductionType, "CIT_DEDUCTION", "Citizen Investment Trust (CIT)", 2, additionalValue1: "DEDUCTION|FIXED|MONTHLY");
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.DeductionType, "LOAN", "Loan", 3, additionalValue1: "DEDUCTION|FIXED|MONTHLY");
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.DeductionType, "ADVANCE", "Advance", 4, additionalValue1: "DEDUCTION|FIXED|MONTHLY");
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.DeductionType, "OTHER", "Other", 5, additionalValue1: "DEDUCTION|FIXED|MONTHLY");

            // AdditionalValue1 = that insurance type's Nepal tax-deduction cap (illustrative --
            // verify against the current Income Tax Act figures before relying on it, same
            // caution as the seeded tax slabs in PayrollSeeder).
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.InsuranceType, "LIFE", "Life Insurance", 1, additionalValue1: "40000");
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.InsuranceType, "HEALTH", "Health/Medical Insurance", 2, additionalValue1: "20000");
            // NPR 10,000 per the FY 2083/84 formula doc -- previously seeded at 25,000 (an earlier
            // guess before that reference doc was provided). Fresh databases get the corrected
            // value; an already-seeded row (create-if-missing) needs a manual PUT to pick it up.
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.InsuranceType, "HOUSING", "Private House Insurance", 3, additionalValue1: "10000");
            // Children's Education Fee deduction: only 25% of the actual annual education expense
            // counts before the NPR 25,000 cap applies (AdditionalValue2 = "25"; every other
            // InsuranceType option leaves this blank, meaning 100% of the actual amount counts).
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.InsuranceType, "EDUCATION", "Children's Education", 4, additionalValue1: "25000", additionalValue2: "25");

            // Pre-run monthly payroll overrides. UNPAID_LEAVE is the one code with special
            // generation-time handling (day-count Quantity); the rest are plain amount/percentage
            // lines. AdditionalValue1's CALCULATE_TYPE segment (2026-07-23, replacing the old bare
            // EARNING/DEDUCTION value) is enforced against the adjustment's Direction -- ADDITION
            // requires Increase, DEDUCTION requires Decrease (EmployeeService.
            // CreateSalaryAdjustmentAsync/UpdateSalaryAdjustmentAsync/
            // CreateBulkSalaryAdjustmentsAsync). TYPE/FREQUENCY are carried for consistency but
            // not enforced (SalaryAdjustment has no catalog percentage lock or FrequencyType
            // field). OTHER is left blank -- it can go either direction, so Direction stays free.
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.SalaryAdjustmentType, "UNPAID_LEAVE", "Unpaid Leave", 1, additionalValue1: "DEDUCTION|FIXED|ONE_TIME");
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.SalaryAdjustmentType, "LATE_FINE", "Late Arrival Fine", 2, additionalValue1: "DEDUCTION|FIXED|ONE_TIME");
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.SalaryAdjustmentType, "OVERTIME", "Overtime", 3, additionalValue1: "ADDITION|FIXED|ONE_TIME");
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.SalaryAdjustmentType, "BONUS", "Bonus", 4, additionalValue1: "ADDITION|FIXED|ONE_TIME");
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.SalaryAdjustmentType, "INCENTIVE", "Incentive", 5, additionalValue1: "ADDITION|FIXED|ONE_TIME");
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.SalaryAdjustmentType, "ARREAR", "Arrear", 6, additionalValue1: "ADDITION|FIXED|ONE_TIME");
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.SalaryAdjustmentType, "OTHER", "Other", 7);

            // Nepal SSF: 31% of Basic Salary total -- 11% deducted from the employee's pay,
            // 20% paid by the employer on top of salary (a CTC cost, never a pay deduction).
            // AdditionalValue1 = the share's percentage of Basic; the salary-calculator module
            // reads these instead of hardcoding the statutory rates.
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.SsfRate, SsfShareCodes.EmployeeShare, "SSF Employee Share (% of Basic)", 1, additionalValue1: "11");
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.SsfRate, SsfShareCodes.EmployerShare, "SSF Employer Share (% of Basic)", 2, additionalValue1: "20");

            // Pre-generation monthly fee overrides. AdditionalValue1 = suggested direction.
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.FeeAdjustmentType, "SPECIAL_DISCOUNT", "Special Discount", 1, additionalValue1: "CREDIT");
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.FeeAdjustmentType, "ADDITIONAL_CHARGE", "Additional Charge", 2, additionalValue1: "CHARGE");
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.FeeAdjustmentType, "FINE", "Fine", 3, additionalValue1: "CHARGE");
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.FeeAdjustmentType, "CARRY_CORRECTION", "Opening Balance / Carry Correction", 4, additionalValue1: "CHARGE");
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.FeeAdjustmentType, "OTHER", "Other", 5);

            // Nepal's 7 federal provinces (2026-07-23) -- near-universal, same reasoning as
            // GuardianRelationship.
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.Province, "KOSHI", "Koshi Province", 1);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.Province, "MADHESH", "Madhesh Province", 2);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.Province, "BAGMATI", "Bagmati Province", 3);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.Province, "GANDAKI", "Gandaki Province", 4);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.Province, "LUMBINI", "Lumbini Province", 5);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.Province, "KARNALI", "Karnali Province", 6);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.Province, "SUDURPASHCHIM", "Sudurpashchim Province", 7);

            // A generic seniority ladder -- illustrative starting point, admin-extensible via
            // POST /api/configs same as every other catalog.
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.EmployeeLevel, "JUNIOR", "Junior", 1);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.EmployeeLevel, "MID", "Mid", 2);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.EmployeeLevel, "SENIOR", "Senior", 3);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.EmployeeLevel, "LEAD", "Lead", 4);
            await EnsureConfigAsync(dbContext, ConfigTypeCodes.EmployeeLevel, "EXECUTIVE", "Executive", 5);

            // Illustrative class periods + breaks used to be seeded here as Config options --
            // moved to TimePeriodSeeder (real TimePeriod rows) 2026-08-03, see that seeder and
            // Docs/time_period_and_class_routine_implementation_guide.md.

            await SeedNepalDistrictsAndLocalLevelsAsync(dbContext);
        }

        // Nepal's 7 provinces / 77 districts / 753 local levels (2017 federal restructuring).
        // Districts below are the full, stable 77 -- low risk, this list hasn't changed since
        // 2017. LocalLevels below is a DELIBERATELY PARTIAL set (80 of 753): the 6 Metropolitan
        // Cities, the 11 Sub-Metropolitan Cities, and one notable municipality/rural municipality
        // per remaining district -- every one of the 77 districts has at least one usable,
        // searchable option immediately. The other ~670 (mostly smaller rural municipalities) are
        // NOT included here: hand-typing all of them from model training data risked baking wrong
        // government records into the database with no authoritative source to check against
        // (unlike, say, PayrollSeeder's clearly-illustrative placeholder tax slabs, a wrong
        // municipality name/mapping here is unlikely to be noticed until an address is wrong).
        // Add the rest via POST /api/configs (TypeCode 1023, AdditionalValue1 = DistrictCode,
        // AdditionalValue2 = ProvinceCode, AdditionalValue3 = LocalLevelTypeCodes value) once you
        // have the official MoFAGA/Election Commission of Nepal list to hand -- see
        // Docs/employee_address_implementation_guide.md. Create-if-missing, same as every other
        // catalog here -- admin corrections to any row below survive restarts.
        private static readonly (string Code, string Label, string ProvinceCode)[] NepalDistricts =
        {
            ("TAPLEJUNG", "Taplejung", "KOSHI"),
            ("PANCHTHAR", "Panchthar", "KOSHI"),
            ("ILAM", "Ilam", "KOSHI"),
            ("JHAPA", "Jhapa", "KOSHI"),
            ("MORANG", "Morang", "KOSHI"),
            ("SUNSARI", "Sunsari", "KOSHI"),
            ("DHANKUTA", "Dhankuta", "KOSHI"),
            ("TERHATHUM", "Terhathum", "KOSHI"),
            ("SANKHUWASABHA", "Sankhuwasabha", "KOSHI"),
            ("BHOJPUR", "Bhojpur", "KOSHI"),
            ("SOLUKHUMBU", "Solukhumbu", "KOSHI"),
            ("OKHALDHUNGA", "Okhaldhunga", "KOSHI"),
            ("KHOTANG", "Khotang", "KOSHI"),
            ("UDAYAPUR", "Udayapur", "KOSHI"),

            ("SAPTARI", "Saptari", "MADHESH"),
            ("SIRAHA", "Siraha", "MADHESH"),
            ("DHANUSHA", "Dhanusha", "MADHESH"),
            ("MAHOTTARI", "Mahottari", "MADHESH"),
            ("SARLAHI", "Sarlahi", "MADHESH"),
            ("RAUTAHAT", "Rautahat", "MADHESH"),
            ("BARA", "Bara", "MADHESH"),
            ("PARSA", "Parsa", "MADHESH"),

            ("SINDHULI", "Sindhuli", "BAGMATI"),
            ("RAMECHHAP", "Ramechhap", "BAGMATI"),
            ("DOLAKHA", "Dolakha", "BAGMATI"),
            ("BHAKTAPUR", "Bhaktapur", "BAGMATI"),
            ("DHADING", "Dhading", "BAGMATI"),
            ("KATHMANDU", "Kathmandu", "BAGMATI"),
            ("KAVREPALANCHOK", "Kavrepalanchok", "BAGMATI"),
            ("LALITPUR", "Lalitpur", "BAGMATI"),
            ("NUWAKOT", "Nuwakot", "BAGMATI"),
            ("RASUWA", "Rasuwa", "BAGMATI"),
            ("SINDHUPALCHOK", "Sindhupalchok", "BAGMATI"),
            ("CHITWAN", "Chitwan", "BAGMATI"),
            ("MAKWANPUR", "Makwanpur", "BAGMATI"),

            ("GORKHA", "Gorkha", "GANDAKI"),
            ("LAMJUNG", "Lamjung", "GANDAKI"),
            ("MANANG", "Manang", "GANDAKI"),
            ("KASKI", "Kaski", "GANDAKI"),
            ("TANAHUN", "Tanahun", "GANDAKI"),
            ("SYANGJA", "Syangja", "GANDAKI"),
            ("BAGLUNG", "Baglung", "GANDAKI"),
            ("PARBAT", "Parbat", "GANDAKI"),
            ("NAWALPUR", "Nawalpur", "GANDAKI"),
            ("MYAGDI", "Myagdi", "GANDAKI"),
            ("MUSTANG", "Mustang", "GANDAKI"),

            ("RUPANDEHI", "Rupandehi", "LUMBINI"),
            ("KAPILVASTU", "Kapilvastu", "LUMBINI"),
            ("ARGHAKHANCHI", "Arghakhanchi", "LUMBINI"),
            ("GULMI", "Gulmi", "LUMBINI"),
            ("PALPA", "Palpa", "LUMBINI"),
            ("PARASI", "Parasi (Nawalparasi West)", "LUMBINI"),
            ("DANG", "Dang", "LUMBINI"),
            ("PYUTHAN", "Pyuthan", "LUMBINI"),
            ("ROLPA", "Rolpa", "LUMBINI"),
            ("RUKUM_EAST", "Rukum East", "LUMBINI"),
            ("BANKE", "Banke", "LUMBINI"),
            ("BARDIYA", "Bardiya", "LUMBINI"),

            ("DOLPA", "Dolpa", "KARNALI"),
            ("MUGU", "Mugu", "KARNALI"),
            ("HUMLA", "Humla", "KARNALI"),
            ("JUMLA", "Jumla", "KARNALI"),
            ("KALIKOT", "Kalikot", "KARNALI"),
            ("RUKUM_WEST", "Rukum West", "KARNALI"),
            ("SALYAN", "Salyan", "KARNALI"),
            ("SURKHET", "Surkhet", "KARNALI"),
            ("DAILEKH", "Dailekh", "KARNALI"),
            ("JAJARKOT", "Jajarkot", "KARNALI"),

            ("KAILALI", "Kailali", "SUDURPASHCHIM"),
            ("ACHHAM", "Achham", "SUDURPASHCHIM"),
            ("DOTI", "Doti", "SUDURPASHCHIM"),
            ("BAJHANG", "Bajhang", "SUDURPASHCHIM"),
            ("BAJURA", "Bajura", "SUDURPASHCHIM"),
            ("KANCHANPUR", "Kanchanpur", "SUDURPASHCHIM"),
            ("DADELDHURA", "Dadeldhura", "SUDURPASHCHIM"),
            ("BAITADI", "Baitadi", "SUDURPASHCHIM"),
            ("DARCHULA", "Darchula", "SUDURPASHCHIM")
        };

        // (Code, Label, DistrictCode, ProvinceCode, Type). See the doc comment above
        // NepalDistricts for why this is 80 of 753, not the full list.
        private static readonly (string Code, string Label, string DistrictCode, string ProvinceCode, string Type)[] NepalLocalLevels =
        {
            // 6 Metropolitan Cities.
            ("KATHMANDU_MC", "Kathmandu Metropolitan City", "KATHMANDU", "BAGMATI", LocalLevelTypeCodes.MetropolitanCity),
            ("POKHARA_MC", "Pokhara Metropolitan City", "KASKI", "GANDAKI", LocalLevelTypeCodes.MetropolitanCity),
            ("LALITPUR_MC", "Lalitpur Metropolitan City", "LALITPUR", "BAGMATI", LocalLevelTypeCodes.MetropolitanCity),
            ("BHARATPUR_MC", "Bharatpur Metropolitan City", "CHITWAN", "BAGMATI", LocalLevelTypeCodes.MetropolitanCity),
            ("BIRATNAGAR_MC", "Biratnagar Metropolitan City", "MORANG", "KOSHI", LocalLevelTypeCodes.MetropolitanCity),
            ("BIRGUNJ_MC", "Birgunj Metropolitan City", "PARSA", "MADHESH", LocalLevelTypeCodes.MetropolitanCity),

            // 11 Sub-Metropolitan Cities.
            ("DHARAN_SMC", "Dharan Sub-Metropolitan City", "SUNSARI", "KOSHI", LocalLevelTypeCodes.SubMetropolitanCity),
            ("ITAHARI_SMC", "Itahari Sub-Metropolitan City", "SUNSARI", "KOSHI", LocalLevelTypeCodes.SubMetropolitanCity),
            ("JANAKPUR_SMC", "Janakpur Sub-Metropolitan City", "DHANUSHA", "MADHESH", LocalLevelTypeCodes.SubMetropolitanCity),
            ("KALAIYA_SMC", "Kalaiya Sub-Metropolitan City", "BARA", "MADHESH", LocalLevelTypeCodes.SubMetropolitanCity),
            ("JITPUR_SIMARA_SMC", "Jitpur Simara Sub-Metropolitan City", "BARA", "MADHESH", LocalLevelTypeCodes.SubMetropolitanCity),
            ("HETAUDA_SMC", "Hetauda Sub-Metropolitan City", "MAKWANPUR", "BAGMATI", LocalLevelTypeCodes.SubMetropolitanCity),
            ("TULSIPUR_SMC", "Tulsipur Sub-Metropolitan City", "DANG", "LUMBINI", LocalLevelTypeCodes.SubMetropolitanCity),
            ("GHORAHI_SMC", "Ghorahi Sub-Metropolitan City", "DANG", "LUMBINI", LocalLevelTypeCodes.SubMetropolitanCity),
            ("NEPALGUNJ_SMC", "Nepalgunj Sub-Metropolitan City", "BANKE", "LUMBINI", LocalLevelTypeCodes.SubMetropolitanCity),
            ("DHANGADHI_SMC", "Dhangadhi Sub-Metropolitan City", "KAILALI", "SUDURPASHCHIM", LocalLevelTypeCodes.SubMetropolitanCity),
            ("SIDDHARTHANAGAR_SMC", "Siddharthanagar Sub-Metropolitan City", "RUPANDEHI", "LUMBINI", LocalLevelTypeCodes.SubMetropolitanCity),

            // One notable municipality/rural municipality per remaining district (63).
            ("PHUNGLING_M", "Phungling Municipality", "TAPLEJUNG", "KOSHI", LocalLevelTypeCodes.Municipality),
            ("PHIDIM_M", "Phidim Municipality", "PANCHTHAR", "KOSHI", LocalLevelTypeCodes.Municipality),
            ("ILAM_M", "Ilam Municipality", "ILAM", "KOSHI", LocalLevelTypeCodes.Municipality),
            ("BHADRAPUR_M", "Bhadrapur Municipality", "JHAPA", "KOSHI", LocalLevelTypeCodes.Municipality),
            ("DHANKUTA_M", "Dhankuta Municipality", "DHANKUTA", "KOSHI", LocalLevelTypeCodes.Municipality),
            ("MYANGLUNG_M", "Myanglung Municipality", "TERHATHUM", "KOSHI", LocalLevelTypeCodes.Municipality),
            ("KHANDBARI_M", "Khandbari Municipality", "SANKHUWASABHA", "KOSHI", LocalLevelTypeCodes.Municipality),
            ("BHOJPUR_M", "Bhojpur Municipality", "BHOJPUR", "KOSHI", LocalLevelTypeCodes.Municipality),
            ("SOLUDUDHKUNDA_M", "Solududhkunda Municipality", "SOLUKHUMBU", "KOSHI", LocalLevelTypeCodes.Municipality),
            ("SIDDHICHARAN_M", "Siddhicharan Municipality", "OKHALDHUNGA", "KOSHI", LocalLevelTypeCodes.Municipality),
            ("DIKTEL_RUPAKOT_MAJHUWAGADHI_M", "Diktel Rupakot Majhuwagadhi Municipality", "KHOTANG", "KOSHI", LocalLevelTypeCodes.Municipality),
            ("TRIYUGA_M", "Triyuga Municipality", "UDAYAPUR", "KOSHI", LocalLevelTypeCodes.Municipality),

            ("RAJBIRAJ_M", "Rajbiraj Municipality", "SAPTARI", "MADHESH", LocalLevelTypeCodes.Municipality),
            ("SIRAHA_M", "Siraha Municipality", "SIRAHA", "MADHESH", LocalLevelTypeCodes.Municipality),
            ("JALESHWAR_M", "Jaleshwar Municipality", "MAHOTTARI", "MADHESH", LocalLevelTypeCodes.Municipality),
            ("MALANGWA_M", "Malangwa Municipality", "SARLAHI", "MADHESH", LocalLevelTypeCodes.Municipality),
            ("GAUR_M", "Gaur Municipality", "RAUTAHAT", "MADHESH", LocalLevelTypeCodes.Municipality),

            ("KAMALAMAI_M", "Kamalamai Municipality", "SINDHULI", "BAGMATI", LocalLevelTypeCodes.Municipality),
            ("MANTHALI_M", "Manthali Municipality", "RAMECHHAP", "BAGMATI", LocalLevelTypeCodes.Municipality),
            ("BHIMESHWAR_M", "Bhimeshwar Municipality", "DOLAKHA", "BAGMATI", LocalLevelTypeCodes.Municipality),
            ("BHAKTAPUR_M", "Bhaktapur Municipality", "BHAKTAPUR", "BAGMATI", LocalLevelTypeCodes.Municipality),
            ("NILKANTHA_M", "Nilkantha Municipality", "DHADING", "BAGMATI", LocalLevelTypeCodes.Municipality),
            ("DHULIKHEL_M", "Dhulikhel Municipality", "KAVREPALANCHOK", "BAGMATI", LocalLevelTypeCodes.Municipality),
            ("BIDUR_M", "Bidur Municipality", "NUWAKOT", "BAGMATI", LocalLevelTypeCodes.Municipality),
            ("UTTARGAYA_RM", "Uttargaya Rural Municipality", "RASUWA", "BAGMATI", LocalLevelTypeCodes.RuralMunicipality),
            ("CHAUTARA_SANGACHOWKGADHI_M", "Chautara Sangachowkgadhi Municipality", "SINDHUPALCHOK", "BAGMATI", LocalLevelTypeCodes.Municipality),

            ("GORKHA_M", "Gorkha Municipality", "GORKHA", "GANDAKI", LocalLevelTypeCodes.Municipality),
            ("BESISAHAR_M", "Besisahar Municipality", "LAMJUNG", "GANDAKI", LocalLevelTypeCodes.Municipality),
            ("CHAME_RM", "Chame Rural Municipality", "MANANG", "GANDAKI", LocalLevelTypeCodes.RuralMunicipality),
            ("BYAS_M", "Byas Municipality", "TANAHUN", "GANDAKI", LocalLevelTypeCodes.Municipality),
            ("PUTALIBAZAR_M", "Putalibazar Municipality", "SYANGJA", "GANDAKI", LocalLevelTypeCodes.Municipality),
            ("BAGLUNG_M", "Baglung Municipality", "BAGLUNG", "GANDAKI", LocalLevelTypeCodes.Municipality),
            ("KUSHMA_M", "Kushma Municipality", "PARBAT", "GANDAKI", LocalLevelTypeCodes.Municipality),
            ("KAWASOTI_M", "Kawasoti Municipality", "NAWALPUR", "GANDAKI", LocalLevelTypeCodes.Municipality),
            ("BENI_M", "Beni Municipality", "MYAGDI", "GANDAKI", LocalLevelTypeCodes.Municipality),
            ("GHARAPJHONG_RM", "Gharapjhong Rural Municipality", "MUSTANG", "GANDAKI", LocalLevelTypeCodes.RuralMunicipality),

            ("KAPILVASTU_M", "Kapilvastu Municipality", "KAPILVASTU", "LUMBINI", LocalLevelTypeCodes.Municipality),
            ("SANDHIKHARKA_M", "Sandhikharka Municipality", "ARGHAKHANCHI", "LUMBINI", LocalLevelTypeCodes.Municipality),
            ("RESUNGA_M", "Resunga Municipality", "GULMI", "LUMBINI", LocalLevelTypeCodes.Municipality),
            ("TANSEN_M", "Tansen Municipality", "PALPA", "LUMBINI", LocalLevelTypeCodes.Municipality),
            ("RAMGRAM_M", "Ramgram Municipality", "PARASI", "LUMBINI", LocalLevelTypeCodes.Municipality),
            ("PYUTHAN_M", "Pyuthan Municipality", "PYUTHAN", "LUMBINI", LocalLevelTypeCodes.Municipality),
            ("LIWANG_M", "Liwang Municipality", "ROLPA", "LUMBINI", LocalLevelTypeCodes.Municipality),
            ("SUNCHHAHARI_RM", "Sunchhahari Rural Municipality", "RUKUM_EAST", "LUMBINI", LocalLevelTypeCodes.RuralMunicipality),
            ("GULARIYA_M", "Gulariya Municipality", "BARDIYA", "LUMBINI", LocalLevelTypeCodes.Municipality),

            ("THULI_BHERI_M", "Thuli Bheri Municipality", "DOLPA", "KARNALI", LocalLevelTypeCodes.Municipality),
            ("CHHAYANATH_RARA_M", "Chhayanath Rara Municipality", "MUGU", "KARNALI", LocalLevelTypeCodes.Municipality),
            ("SIMKOT_RM", "Simkot Rural Municipality", "HUMLA", "KARNALI", LocalLevelTypeCodes.RuralMunicipality),
            ("CHANDANNATH_M", "Chandannath Municipality", "JUMLA", "KARNALI", LocalLevelTypeCodes.Municipality),
            ("KHANDACHAKRA_M", "Khandachakra Municipality", "KALIKOT", "KARNALI", LocalLevelTypeCodes.Municipality),
            ("MUSIKOT_M", "Musikot Municipality", "RUKUM_WEST", "KARNALI", LocalLevelTypeCodes.Municipality),
            ("SHARADA_M", "Sharada Municipality", "SALYAN", "KARNALI", LocalLevelTypeCodes.Municipality),
            ("BIRENDRANAGAR_M", "Birendranagar Municipality", "SURKHET", "KARNALI", LocalLevelTypeCodes.Municipality),
            ("NARAYAN_M", "Narayan Municipality", "DAILEKH", "KARNALI", LocalLevelTypeCodes.Municipality),
            ("BHERI_M", "Bheri Municipality", "JAJARKOT", "KARNALI", LocalLevelTypeCodes.Municipality),

            ("MANGALSEN_M", "Mangalsen Municipality", "ACHHAM", "SUDURPASHCHIM", LocalLevelTypeCodes.Municipality),
            ("DIPAYAL_SILGADHI_M", "Dipayal Silgadhi Municipality", "DOTI", "SUDURPASHCHIM", LocalLevelTypeCodes.Municipality),
            ("JAYA_PRITHVI_M", "Jaya Prithvi Municipality", "BAJHANG", "SUDURPASHCHIM", LocalLevelTypeCodes.Municipality),
            ("BADIMALIKA_M", "Badimalika Municipality", "BAJURA", "SUDURPASHCHIM", LocalLevelTypeCodes.Municipality),
            ("BHIMDATTA_M", "Bhimdatta Municipality", "KANCHANPUR", "SUDURPASHCHIM", LocalLevelTypeCodes.Municipality),
            ("AMARGADHI_M", "Amargadhi Municipality", "DADELDHURA", "SUDURPASHCHIM", LocalLevelTypeCodes.Municipality),
            ("DASHARATHCHAND_M", "Dasharathchand Municipality", "BAITADI", "SUDURPASHCHIM", LocalLevelTypeCodes.Municipality),
            ("MAHAKALI_M", "Mahakali Municipality", "DARCHULA", "SUDURPASHCHIM", LocalLevelTypeCodes.Municipality)
        };

        private static async Task SeedNepalDistrictsAndLocalLevelsAsync(ApplicationDbContext dbContext)
        {
            var order = 1;
            foreach (var district in NepalDistricts)
            {
                await EnsureConfigAsync(dbContext, ConfigTypeCodes.District, district.Code, district.Label, order, district.ProvinceCode);
                order++;
            }

            order = 1;
            foreach (var localLevel in NepalLocalLevels)
            {
                await EnsureConfigAsync(dbContext, ConfigTypeCodes.LocalLevel, localLevel.Code, localLevel.Label, order, localLevel.DistrictCode, localLevel.ProvinceCode, localLevel.Type);
                order++;
            }
        }

        private static async Task NormalizeFeeCategoryFrequenciesAsync(ApplicationDbContext dbContext)
        {
            var feeCategoryConfigs = await dbContext.Configs
                .Where(config => config.TypeCode == ConfigTypeCodes.FeeCategory)
                .ToListAsync();

            var anyNormalized = false;
            foreach (var feeCategoryConfig in feeCategoryConfigs)
            {
                var currentValue = feeCategoryConfig.AdditionalValue1?.Trim();

                string normalizedValue = null;
                if (string.IsNullOrWhiteSpace(currentValue) || string.Equals(currentValue, "Monthly", StringComparison.OrdinalIgnoreCase))
                {
                    normalizedValue = FeeFrequencyCodes.Monthly;
                }
                else if (string.Equals(currentValue, "Annual", StringComparison.OrdinalIgnoreCase))
                {
                    normalizedValue = FeeFrequencyCodes.Annual;
                }
                else if (string.Equals(currentValue, "OneTime", StringComparison.OrdinalIgnoreCase))
                {
                    normalizedValue = FeeFrequencyCodes.OneTime;
                }

                if (normalizedValue != null && normalizedValue != currentValue)
                {
                    feeCategoryConfig.AdditionalValue1 = normalizedValue;
                    anyNormalized = true;
                }
            }

            if (anyNormalized)
            {
                await dbContext.SaveChangesAsync();
            }
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
