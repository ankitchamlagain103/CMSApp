using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Identity.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Guid? UserId
        {
            get
            {
                // JwtTokenGenerator writes the user id as "sub", but the JWT bearer middleware's
                // default inbound claim mapping renames "sub" to ClaimTypes.NameIdentifier before
                // it reaches HttpContext.User -- so check the mapped name first, raw "sub" second.
                var userIdClaimValue = GetClaimValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaimValue))
                {
                    userIdClaimValue = GetClaimValue(JwtRegisteredClaimNames.Sub);
                }

                if (string.IsNullOrEmpty(userIdClaimValue))
                {
                    return null;
                }

                if (Guid.TryParse(userIdClaimValue, out var parsedUserId))
                {
                    return parsedUserId;
                }

                return null;
            }
        }

        public string UserName
        {
            get
            {
                var userName = GetClaimValue("userName");
                return userName;
            }
        }

        public bool IsAuthenticated
        {
            get
            {
                var currentUser = _httpContextAccessor.HttpContext?.User;
                var isAuthenticated = currentUser?.Identity?.IsAuthenticated ?? false;
                return isAuthenticated;
            }
        }

        private string GetClaimValue(string claimType)
        {
            var currentUser = _httpContextAccessor.HttpContext?.User;
            if (currentUser == null)
            {
                return null;
            }

            var claim = currentUser.FindFirstValue(claimType);
            return claim;
        }
    }
}
