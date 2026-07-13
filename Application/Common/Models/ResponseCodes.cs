namespace Application.Common.Models
{
    public static class ResponseCodes
    {
        public const string Success = "SUCCESS";
        public const string ValidationError = "VALIDATION_ERROR";
        public const string NotFound = "NOT_FOUND";
        public const string Conflict = "CONFLICT";
        public const string Unauthorized = "UNAUTHORIZED";
        public const string Forbidden = "FORBIDDEN";
        public const string TooManyRequests = "TOO_MANY_REQUESTS";
        public const string ServerError = "SERVER_ERROR";
    }
}
