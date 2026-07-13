using Domain.Entities;

namespace Infrastructure.Identity
{
    public class RefreshToken : AuditableEntity
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Token { get; set; }
        public DateTime ExpiresAtUtc { get; set; }
        public DateTime? RevokedAtUtc { get; set; }
        public string ReplacedByToken { get; set; }

        public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc;
        public bool IsRevoked => RevokedAtUtc.HasValue;
        public bool IsActive => !IsExpired && !IsRevoked;

        public virtual ApplicationUser ApplicationUser { get; set; }
    }
}
