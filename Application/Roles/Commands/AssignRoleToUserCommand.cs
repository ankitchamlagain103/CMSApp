namespace Application.Roles.Commands
{
    public class AssignRoleToUserCommand
    {
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
    }
}
