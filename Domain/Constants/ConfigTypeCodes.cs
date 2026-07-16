namespace Domain.Constants
{
    // Fixed TypeCodes for the config-backed catalogs the student-management feature validates
    // against. Grade/Section/Subject/GuardianRelationship/TeacherQualification are deliberately
    // NOT database tables -- they are dropdown catalogs in ConfigType/Config, and the entities
    // store the option's Code (validated in the services against these TypeCodes). Seeded by
    // ConfigCatalogSeeder; keep these values in sync with it.
    public static class ConfigTypeCodes
    {
        public const int Grade = 1001;
        public const int Section = 1002;
        public const int Subject = 1003;
        public const int GuardianRelationship = 1004;
        public const int TeacherQualification = 1005;
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
    }
}
