namespace Domain.Common.Filters
{
    // Repository-side filter for GetMenusQuery -- same rationale as StudentFilter.
    public class MenuFilter
    {
        public string MenuType { get; set; }
        public string MenuFor { get; set; }
        public string Search { get; set; }
        public int? ParentId { get; set; }
        public bool? IsHidden { get; set; }
    }
}
