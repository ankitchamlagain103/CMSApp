namespace Application.Roles.Commands
{
    public class UpdateRoleCommand
    {
        public string Name { get; set; }
        public string Description { get; set; }

        // Optional. Domain.Constants.MenuAudience value (ADMIN/USER/BOTH); null/empty leaves the
        // role's current UserType unchanged.
        public string UserType { get; set; }
    }
}
