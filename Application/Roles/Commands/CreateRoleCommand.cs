namespace Application.Roles.Commands
{
    public class CreateRoleCommand
    {
        public string Name { get; set; }
        public string Description { get; set; }

        // Optional. Domain.Constants.MenuAudience value (ADMIN/USER/BOTH); defaults to BOTH when
        // omitted. Lets the frontend filter the menu-claims picker to menus relevant to this
        // role's audience -- purely a UI aid, not enforced by AuthorizedAction.
        public string UserType { get; set; }
    }
}
