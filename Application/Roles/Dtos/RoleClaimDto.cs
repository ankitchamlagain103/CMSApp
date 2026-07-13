namespace Application.Roles.Dtos
{
    public class RoleClaimDto
    {
        public int Id { get; set; }
        public Guid RoleId { get; set; }
        public int MenuId { get; set; }
        public string MenuCode { get; set; }
        public string MenuDisplayName { get; set; }
    }
}
