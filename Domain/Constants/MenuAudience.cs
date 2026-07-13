namespace Domain.Constants
{
    public static class MenuAudience
    {
        public const string Admin = "ADMIN";
        public const string User = "USER";
        public const string Both = "BOTH";

        public static readonly string[] All = { Admin, User, Both };
    }
}
