namespace Application.Users.Dtos
{
    // A menu/permission granted directly to one user (2026-08-07), bypassing roles entirely --
    // additive on top of whatever the user's roles already grant, never a substitute. Same shape
    // as Application.Roles.Dtos.RoleClaimDto, keyed by UserId instead of RoleId.
    public class UserClaimDto
    {
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public int MenuId { get; set; }
        public string MenuCode { get; set; }
        public string MenuDisplayName { get; set; }
    }
}
