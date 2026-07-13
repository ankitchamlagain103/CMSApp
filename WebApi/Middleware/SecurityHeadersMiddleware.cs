namespace WebApi.Middleware
{
    // Adds the standard defensive response headers to every response. Deliberately minimal for a
    // JSON API: no Content-Security-Policy, because the only HTML this host serves is Swagger UI
    // in Development and a CSP strict enough to matter would break it.
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        public SecurityHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var headers = context.Response.Headers;

            // Stops browsers from MIME-sniffing a response away from the declared content type.
            headers["X-Content-Type-Options"] = "nosniff";

            // An API has no business being rendered inside a frame -- blocks clickjacking.
            headers["X-Frame-Options"] = "DENY";

            // API responses should never leak their URLs (which can carry ids) as referrers.
            headers["Referrer-Policy"] = "no-referrer";

            await _next(context);
        }
    }
}
