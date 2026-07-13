using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Identity
{
    public class ApplicationUserToken : IdentityUserToken<Guid>
    {
        public DateTime RefreshTokenExpiryDate { get; set; }
        public DateTime? TokenRefreshTokenRevokedDate { get; set; }
        public ApplicationUser ApplicationUser { get; set; }

        public bool IsExpired => DateTime.UtcNow >= RefreshTokenExpiryDate;
        public bool IsRevoked => TokenRefreshTokenRevokedDate.HasValue;
    }
}
