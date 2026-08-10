using Application.AccessLogs;
using Application.AccessLogs.Commands;
using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Enums;
using Infrastructure.Common;
using Infrastructure.Identity;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace WebApi.Filters
{
    public class AuthorizedAction : ActionFilterAttribute
    {
        private readonly IConfiguration _configuration;

        public AuthorizedAction(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var actionAllowsAnonymous = context.ActionDescriptor.EndpointMetadata.Any(metadata => metadata is AllowAnonymousAttribute);
            if (actionAllowsAnonymous)
            {
                await next();
                return;
            }

            var currentUserService = context.HttpContext.RequestServices.GetRequiredService<ICurrentUserService>();
            var currentUserId = currentUserService.UserId;
            if (currentUserId == null)
            {
                context.Result = BuildForbiddenResult();
                return;
            }

            var dbContext = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();

            // The user row is read from the database, not the JWT's claims, so a demotion, a
            // soft delete (the global query filter hides deleted users), or an allowlist change
            // takes effect on the very next request instead of waiting for the token to expire.
            var currentUser = await GetUserAsync(dbContext, currentUserId.Value, context.HttpContext.RequestAborted);
            if (currentUser == null)
            {
                context.Result = BuildForbiddenResult();
                return;
            }

            // Per-user IP restriction bites before anything else -- including the SuperAdmin
            // bypass, so a restricted SuperAdmin is held to their allowlist too.
            if (currentUser.IsIpRestricted)
            {
                var remoteIpAddress = context.HttpContext.Connection.RemoteIpAddress;
                var ipIsAllowed = IpAllowlistChecker.IsAllowed(remoteIpAddress, currentUser.UserIpAllowed);
                if (!ipIsAllowed)
                {
                    context.Result = BuildForbiddenResult();
                    return;
                }
            }

            var controllerName = context.RouteData.Values["controller"]?.ToString();
            var actionName = context.RouteData.Values["action"]?.ToString();

            // A SuperAdmin bypasses the per-menu permission lookup entirely.
            if (currentUser.UserType == UserType.SuperAdmin)
            {
                await LogCriticalAccessIfConfiguredAsync(context, currentUserId.Value, currentUserService.UserName, controllerName, actionName);
                await next();
                return;
            }

            var defaultEnabledMenus = GetConfiguredActions("DefaultEnabledMenu");
            if (defaultEnabledMenus.TryGetValue(controllerName, out var defaultEnabledActions))
            {
                if (defaultEnabledActions.Contains(actionName))
                {
                    await LogCriticalAccessIfConfiguredAsync(context, currentUserId.Value, currentUserService.UserName, controllerName, actionName);
                    await next();
                    return;
                }
            }

            // Every authenticated user can look up their own account via GET /api/users/{id} --
            // no USER_DETAIL grant needed, same as GetUserRolesAsync needing no grant to read the
            // caller's own permission tree. Scoped to exactly this one action, and only when the
            // route id matches the caller's own id -- looking up anyone else still requires
            // USER_DETAIL below.
            if (IsSelfUserLookup(controllerName, actionName, context.ActionArguments, currentUserId.Value))
            {
                await LogCriticalAccessIfConfiguredAsync(context, currentUserId.Value, currentUserService.UserName, controllerName, actionName);
                await next();
                return;
            }

            var isAuthorized = await ValidateUserRoleClaimsAsync(dbContext, currentUserId.Value, controllerName, actionName, context.HttpContext.RequestAborted);
            if (!isAuthorized)
            {
                context.Result = BuildForbiddenResult();
                return;
            }

            await LogCriticalAccessIfConfiguredAsync(context, currentUserId.Value, currentUserService.UserName, controllerName, actionName);
            await next();
        }

        // Writes a SystemAccessLog row when the current controller/action pair is listed in the
        // "CriticalChanges" configuration section (same shape as "DefaultEnabledMenu"). Only
        // authorized requests reach this point, so the log records accesses that actually ran.
        private async Task LogCriticalAccessIfConfiguredAsync(ActionExecutingContext context, Guid userId, string userName, string controllerName, string actionName)
        {
            var criticalChanges = GetConfiguredActions("CriticalChanges");
            if (!criticalChanges.TryGetValue(controllerName, out var criticalActions))
            {
                return;
            }

            if (!criticalActions.Contains(actionName))
            {
                return;
            }

            var httpContext = context.HttpContext;
            try
            {
                var systemAccessLogService = httpContext.RequestServices.GetRequiredService<ISystemAccessLogService>();

                var accessLogCommand = new CreateSystemAccessLogCommand
                {
                    UserId = userId,
                    UserName = userName,
                    Controller = controllerName,
                    Action = actionName,
                    HttpMethod = httpContext.Request.Method,
                    Url = httpContext.Request.Path + httpContext.Request.QueryString,
                    IpAddress = httpContext.Connection.RemoteIpAddress?.ToString()
                };

                // CancellationToken.None: a client disconnect must not be able to skip the audit
                // entry for a critical change that is about to execute.
                await systemAccessLogService.LogAccessAsync(accessLogCommand, CancellationToken.None);
            }
            catch (Exception exception)
            {
                // Access logging must never take the actual request down with it.
                var logger = httpContext.RequestServices.GetRequiredService<ILogger<AuthorizedAction>>();
                logger.LogWarning(exception, "Failed to write system access log for {Controller}/{Action}.", controllerName, actionName);
            }
        }

        private static bool IsSelfUserLookup(string controllerName, string actionName, IDictionary<string, object> actionArguments, Guid currentUserId)
        {
            if (controllerName != "Users" || actionName != "GetUserById")
            {
                return false;
            }

            if (!actionArguments.TryGetValue("id", out var idArgument) || idArgument is not Guid routeUserId)
            {
                return false;
            }

            return routeUserId == currentUserId;
        }

        private static async Task<ApplicationUser> GetUserAsync(ApplicationDbContext dbContext, Guid userId, CancellationToken cancellationToken)
        {
            var user = await dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(applicationUser => applicationUser.Id == userId, cancellationToken);

            return user;
        }

        private static async Task<bool> ValidateUserRoleClaimsAsync(ApplicationDbContext dbContext, Guid userId, string controllerName, string actionName, CancellationToken cancellationToken)
        {
            try
            {
                var menuIds = new HashSet<int>();

                var roleIds = await dbContext.UserRoles
                    .Where(userRole => userRole.UserId == userId)
                    .Select(userRole => userRole.RoleId)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                if (roleIds.Count > 0)
                {
                    var roleMenuIds = await dbContext.RoleClaims
                        .Where(roleClaim => roleIds.Contains(roleClaim.RoleId))
                        .Select(roleClaim => roleClaim.MenuId)
                        .Distinct()
                        .ToListAsync(cancellationToken);

                    foreach (var roleMenuId in roleMenuIds)
                    {
                        menuIds.Add(roleMenuId);
                    }
                }

                // Per-user overrides (2026-08-07) -- additive on top of role grants, never a
                // substitute, so a user with zero roles but a direct menu grant still passes.
                var directMenuIds = await dbContext.UserClaims
                    .Where(userClaim => userClaim.UserId == userId)
                    .Select(userClaim => userClaim.MenuId)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                foreach (var directMenuId in directMenuIds)
                {
                    menuIds.Add(directMenuId);
                }

                if (menuIds.Count == 0)
                {
                    return false;
                }

                var isAuthorized = await dbContext.Menus
                    .AnyAsync(menu => menuIds.Contains(menu.Id) && menu.Controller == controllerName && menu.Action == actionName, cancellationToken);

                return isAuthorized;
            }
            catch (OperationCanceledException)
            {
                // The client is gone -- let the cancellation bubble up rather than mapping it to
                // a misleading "not authorized" answer.
                throw;
            }
            catch
            {
                return false;
            }
        }

        private Dictionary<string, string[]> GetConfiguredActions(string sectionName)
        {
            var section = _configuration.GetSection(sectionName).Get<Dictionary<string, string>>();
            if (section == null)
            {
                return new Dictionary<string, string[]>();
            }

            var menuDictionary = new Dictionary<string, string[]>();
            foreach (var entry in section)
            {
                var actionNames = entry.Value.Split(',', StringSplitOptions.RemoveEmptyEntries);
                menuDictionary.Add(entry.Key, actionNames);
            }

            return menuDictionary;
        }

        private static ObjectResult BuildForbiddenResult()
        {
            var forbiddenResponse = CommonResponse<object>.Fail(ResponseCodes.Forbidden, "You are not authorized to perform this action.");

            var forbiddenResult = new ObjectResult(forbiddenResponse)
            {
                StatusCode = StatusCodes.Status403Forbidden
            };

            return forbiddenResult;
        }
    }
}
