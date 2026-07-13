using System.Net;
using System.Text.Json;
using Application.Common.Models;
using Application.ErrorLogs;
using Application.ErrorLogs.Commands;

namespace WebApi.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IServiceScopeFactory serviceScopeFactory)
        {
            _next = next;
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (OperationCanceledException) when (httpContext.RequestAborted.IsCancellationRequested)
            {
                // The client disconnected and the request was cancelled cooperatively. Nobody is
                // waiting for a response, and this is not a server fault -- log quietly and stop.
                _logger.LogInformation("Request {Path} was cancelled by the client.", httpContext.Request.Path);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Unhandled exception occurred while processing the request.");
                await PersistErrorLogAsync(exception, httpContext);
                await WriteErrorResponseAsync(httpContext);
            }
        }

        private async Task PersistErrorLogAsync(Exception exception, HttpContext httpContext)
        {
            try
            {
                // A fresh scope, not httpContext.RequestServices: the request's own scoped
                // DbContext may be the thing that just failed, so it can't be trusted to save
                // the error row. CancellationToken.None because the log must be written even if
                // the client has already disconnected.
                using var scope = _serviceScopeFactory.CreateScope();
                var errorLogService = scope.ServiceProvider.GetRequiredService<IErrorLogService>();

                var errorLogCommand = new CreateErrorLogCommand
                {
                    ExceptionType = exception.GetType().FullName,
                    Message = exception.Message,
                    StackTrace = exception.StackTrace,
                    Path = httpContext.Request.Path
                };

                await errorLogService.LogErrorAsync(errorLogCommand, CancellationToken.None);
            }
            catch (Exception loggingException)
            {
                // Persisting the error must never replace the original error response -- e.g.
                // when the database itself is down, which is exactly when errors spike.
                _logger.LogWarning(loggingException, "Failed to persist the error log entry.");
            }
        }

        private static Task WriteErrorResponseAsync(HttpContext httpContext)
        {
            var errorResponse = CommonResponse<object>.Fail(ResponseCodes.ServerError, "An unexpected error occurred. Please try again later.");

            httpContext.Response.ContentType = "application/json";
            httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var jsonResponse = JsonSerializer.Serialize(errorResponse);
            return httpContext.Response.WriteAsync(jsonResponse);
        }
    }
}
