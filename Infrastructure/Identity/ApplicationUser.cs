using Domain.Entities.Interfaces;
using Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Identity
{
    public class ApplicationUser : IdentityUser<Guid>, ISoftDeleteAuditableEntity
    {
        public UserType UserType { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public Gender Gender { get; set; }
        public DateTime? Dob { get; set; }
        public bool IsTosAgreed { get; set; }
        public bool IsActive { get; set; }
        public bool IsIpRestricted { get; set; }
        public string UserIpAllowed { get; set; }
        public string PhoneCountryCode { get; set; }
        public string CountryIso3 { get; set; }
        public DateTimeOffset? LastLoginTs { get; set; }
        public DateTimeOffset? LastPasswordChangedTs { get; set; }
        public string CreatedBy { get; set; }
        public DateTimeOffset CreatedTs { get; set; }
        public string UpdatedBy { get; set; }
        public DateTimeOffset? UpdatedTs { get; set; }
        public bool IsDeleted { get; set; }
        public string DeletedBy { get; set; }
        public DateTimeOffset? DeletedTs { get; set; }

        public virtual ICollection<ApplicationUserToken> ApplicationUserTokens { get; set; } = new List<ApplicationUserToken>();
        public virtual ICollection<ApplicationUserRole> ApplicationUserRoles { get; set; } = new List<ApplicationUserRole>();
        public virtual ICollection<ApplicationUserLogin> ApplicationUserLogins { get; set; } = new List<ApplicationUserLogin>();
        public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    }
}
