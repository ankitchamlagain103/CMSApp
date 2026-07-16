namespace Domain.Constants
{
    // The well-known JobPosition codes the teacher-profile eligibility rule checks against --
    // only these three positions (alongside EmployeeCategoryCodes.Academic) may have a Teacher
    // profile.
    public static class JobPositionCodes
    {
        public const string Teacher = "TEACHER";
        public const string Principal = "PRINCIPAL";
        public const string VicePrincipal = "VICE_PRINCIPAL";
    }
}
