using Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Identity
{
    public class ApplicationRoleClaim : IdentityRoleClaim<Guid>
    {
        public int MenuId { get; set; }
        public Menu Menu { get; set; }
        public ApplicationRole ApplicationRole { get; set; }
    }
}
