namespace Domain.Constants
{
    // The well-known JobPosition codes used (alongside EmployeeCategoryCodes.Academic) by
    // EmployeeRoleHelper.IsTeachingStaff to derive whether an Employee counts as "teaching staff"
    // for the dashboard widget / global search -- a read-only predicate, not a gate on any action
    // (any employee may have TeachingLicenseNo/ExperienceYears/Specialization set regardless).
    public static class JobPositionCodes
    {
        public const string Teacher = "TEACHER";
        public const string Principal = "PRINCIPAL";
        public const string VicePrincipal = "VICE_PRINCIPAL";
    }
}
