using Domain.Enums;

namespace Application.Common.Models
{
    // Input to IIdentityService.ProvisionPortalAccountAsync -- the shared primitive behind
    // "create a login for this Employee/Student record" (2026-07-27). RoleIds is the Employee
    // path (admin picks from GET /api/roles, same shape as CreateUserCommand.RoleIds); RoleNames
    // is the Student path (always the fixed RoleNames.Student, no admin choice). Both are
    // optional and additive -- whichever the caller populates gets resolved.
    public class ProvisionPortalAccountRequest
    {
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public Gender Gender { get; set; }
        public List<Guid> RoleIds { get; set; } = new List<Guid>();
        public List<string> RoleNames { get; set; } = new List<string>();
    }
}
