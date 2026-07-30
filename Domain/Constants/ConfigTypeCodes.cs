namespace Domain.Constants
{
    // Fixed TypeCodes for the config-backed catalogs the student-management feature validates
    // against. Grade/Section/Subject/GuardianRelationship/EmployeeQualification are deliberately
    // NOT database tables -- they are dropdown catalogs in ConfigType/Config, and the entities
    // store the option's Code (validated in the services against these TypeCodes). Seeded by
    // ConfigCatalogSeeder; keep these values in sync with it.
    public static class ConfigTypeCodes
    {
        public const int Grade = 1001;
        public const int Section = 1002;
        public const int Subject = 1003;
        public const int GuardianRelationship = 1004;

        // Renamed 2026-07-23 (was TeacherQualification) -- qualifications moved off Teacher onto
        // Employee generically, this catalog was never actually teaching-specific.
        public const int EmployeeQualification = 1005;
        public const int DocumentType = 1006;
        public const int StudentDocumentType = 1007;
        public const int DiscountType = 1008;
        public const int ScholarshipType = 1009;
        public const int FeeCategory = 1010;
        public const int EmployeeCategory = 1011;
        public const int JobPosition = 1012;
        public const int SalaryComponentType = 1013;
        public const int DeductionType = 1014;
        public const int InsuranceType = 1015;
        public const int SalaryAdjustmentType = 1016;
        public const int FeeAdjustmentType = 1017;
        public const int SsfRate = 1018;

        // Employee "org" fields (2026-07-23) -- Branch stays type-only (school-specific, admin
        // created via POST /api/configs, same split as Grade/Section); Province and Level are
        // seeded with a default option set since they're near-universal (Nepal's 7 federal
        // provinces; a generic Junior/Mid/Senior/Lead/Executive ladder).
        public const int Branch = 1019;
        public const int Province = 1020;
        public const int EmployeeLevel = 1021;

        // Employee address chain (2026-07-24), extending Province into a full Nepal address.
        // District.AdditionalValue1 = its ProvinceCode. LocalLevel.AdditionalValue1 =
        // its DistrictCode, AdditionalValue2 = its ProvinceCode (denormalized, so a UI can
        // reverse-map both levels from one LocalLevel option without a second lookup),
        // AdditionalValue3 = its type (Domain/Constants/LocalLevelTypeCodes). Both near-universal
        // like Province, so both get seeded default option rows by ConfigCatalogSeeder.
        public const int District = 1022;
        public const int LocalLevel = 1023;
    }
}
