using Domain.Entities.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Identity
{
    public class ApplicationRole : IdentityRole<Guid>, IAuditableEntity
    {
        public string Description { get; set; }
        public string CreatedBy { get; set; }
        public DateTimeOffset CreatedTs { get; set; }
        public string UpdatedBy { get; set; }
        public DateTimeOffset? UpdatedTs { get; set; }
        public virtual ICollection<ApplicationUserRole> ApplicationUserRoles { get; set; } = new List<ApplicationUserRole>();
        public ICollection<ApplicationRoleClaim> ApplicationRoleClaims { get; set; } = new List<ApplicationRoleClaim>();
    }
}
