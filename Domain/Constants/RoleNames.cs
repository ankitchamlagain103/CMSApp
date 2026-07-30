namespace Domain.Constants
{
    public static class RoleNames
    {
        public const string SuperAdmin = "SuperAdmin";
        public const string Admin = "Admin";
        public const string User = "User";

        // Fixed self-service role for a student portal account (2026-07-27) -- system-assigned,
        // never admin-picked, unlike Employee accounts. Starts with zero granted permissions, same
        // as Admin/User until a real "view my results/schedule" module exists to grant it against.
        public const string Student = "Student";

        public static readonly string[] All = { SuperAdmin, Admin, User, Student };
    }
}
