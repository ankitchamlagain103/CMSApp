using Domain.Entities.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Identity
{
    public class ApplicationRole : IdentityRole<Guid>, IAuditableEntity
    {
        public string Description { get; set; }

        // Not Domain.Enums.UserType (SuperAdmin/Admin/User) -- this is the MenuAudience
        // (Domain.Constants.MenuAudience: ADMIN/USER/BOTH) the role targets, named "UserType"
        // per explicit product request so the frontend can filter which menus are offered when
        // building this role's permission tree (e.g. a role tagged USER only offers USER/BOTH
        // menus in the picker). Purely a UI filtering aid -- AuthorizedAction never reads it,
        // and it does not gate anything server-side the way Menu.MenuFor's audience check does.
        public string UserType { get; set; }

        public string CreatedBy { get; set; }
        public DateTimeOffset CreatedTs { get; set; }
        public string UpdatedBy { get; set; }
        public DateTimeOffset? UpdatedTs { get; set; }
        public virtual ICollection<ApplicationUserRole> ApplicationUserRoles { get; set; } = new List<ApplicationUserRole>();
        public ICollection<ApplicationRoleClaim> ApplicationRoleClaims { get; set; } = new List<ApplicationRoleClaim>();
    }
}
