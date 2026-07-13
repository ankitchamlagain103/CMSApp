namespace Application.Menus.Queries
{
    public class GetMenusQuery
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string MenuType { get; set; }
        public string MenuFor { get; set; }
    }
}
