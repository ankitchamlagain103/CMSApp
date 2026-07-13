using Domain.Entities.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Identity
{
    public class ApplicationUserLogin : IdentityUserLogin<Guid>, IAuditableEntity
    {
        public string CreatedBy { get; set; }
        public DateTimeOffset CreatedTs { get; set; }
        public string UpdatedBy { get; set; }
        public DateTimeOffset? UpdatedTs { get; set; }
        public ApplicationUser ApplicationUser { get; set; }
    }
}
