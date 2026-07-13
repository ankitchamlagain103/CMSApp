using System.Threading.RateLimiting;
using Application.Common.Models;
using Microsoft.AspNetCore.RateLimiting;

namespace WebApi.Extensions
{
    // Fixed-window limiter partitioned by client IP, applied to the Auth endpoints via
    // [EnableRateLimiting(AuthPolicyName)] -- they are [AllowAnonymous], so they are the
    // brute-force / email-spam surface nothing else protects. Limits come from the
    // "RateLimiting" configuration section.
    //
    // The partition key is HttpContext.Connection.RemoteIpAddress: behind a reverse proxy every
    // caller shares the proxy's address, so configure ForwardedHeadersMiddleware in such a
    // deployment or the whole userbase shares one bucket (same caveat as the IP allowlist).
    public static class RateLimitingExtensions
    {
        public const string AuthPolicyName = "Auth";

        public static IServiceCollection AddAuthRateLimiting(this IServiceCollection services, IConfiguration configuration)
        {
            var permitLimit = configuration.GetValue<int?>("RateLimiting:AuthPermitLimit") ?? 10;
            var windowSeconds = configuration.GetValue<int?>("RateLimiting:AuthWindowSeconds") ?? 60;

            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.OnRejected = WriteRejectionResponseAsync;
                options.AddPolicy(AuthPolicyName, httpContext => BuildAuthPartition(httpContext, permitLimit, windowSeconds));
            });

            return services;
        }

        private static RateLimitPartition<string> BuildAuthPartition(HttpContext httpContext, int permitLimit, int windowSeconds)
        {
            var partitionKey = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            var partition = RateLimitPartition.GetFixedWindowLimiter(partitionKey, key => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromSeconds(windowSeconds),
                QueueLimit = 0
            });

            return partition;
        }

        // Rejected requests still get the standard response envelope, not the framework's empty
        // 429 body, so clients can branch on ResponseCode the same way as for every other failure.
        private static ValueTask WriteRejectionResponseAsync(OnRejectedContext context, CancellationToken cancellationToken)
        {
            var rejectionResponse = CommonResponse<object>.Fail(ResponseCodes.TooManyRequests, "Too many requests. Please try again later.");

            context.HttpContext.Response.ContentType = "application/json";
            var writeTask = context.HttpContext.Response.WriteAsJsonAsync(rejectionResponse, cancellationToken);
            return new ValueTask(writeTask);
        }
    }
}
