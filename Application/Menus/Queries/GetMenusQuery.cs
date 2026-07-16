namespace Application.Menus.Queries
{
    // Search matches Code/DisplayName.
    public class GetMenusQuery
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string MenuType { get; set; }
        public string MenuFor { get; set; }
        public string Search { get; set; }
        public int? ParentId { get; set; }
        public bool? IsHidden { get; set; }
    }
}
