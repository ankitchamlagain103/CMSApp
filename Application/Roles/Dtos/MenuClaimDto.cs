namespace Application.Roles.Dtos
{
    public class MenuClaimDto
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string DisplayName { get; set; }
        public string Url { get; set; }
        public string Icon { get; set; }
        public string MenuType { get; set; }
        public int? ParentId { get; set; }
        public int Order { get; set; }
        public bool IsHidden { get; set; }
        public bool HasChildren { get; set; }
        public List<MenuClaimDto> Children { get; set; } = new List<MenuClaimDto>();
    }
}
