namespace Application.Menus.Dtos
{
    public class MenuDto
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string DisplayName { get; set; }
        public string Url { get; set; }
        public string Icon { get; set; }
        public string MenuType { get; set; }
        public string Controller { get; set; }
        public string Action { get; set; }
        public int? ParentId { get; set; }
        public string MenuFor { get; set; }
        public int Order { get; set; }
        public bool IsHidden { get; set; }
    }
}
