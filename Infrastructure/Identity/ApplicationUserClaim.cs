using Domain.Entities;
using Domain.Entities.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Identity
{
    public class ApplicationUserClaim : IdentityUserClaim<Guid>, IAuditableEntity
    {
        public int MenuId { get; set; }
        public Menu Menu { get; set; }
        public string CreatedBy { get; set; }
        public DateTimeOffset CreatedTs { get; set; }
        public string UpdatedBy { get; set; }
        public DateTimeOffset? UpdatedTs { get; set; }
    }
}
