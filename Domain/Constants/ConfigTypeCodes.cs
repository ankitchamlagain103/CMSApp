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
    }
}
