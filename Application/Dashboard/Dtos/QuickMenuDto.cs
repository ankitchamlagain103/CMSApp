namespace Application.Dashboard.Dtos
{
    public class QuickMenuDto
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string DisplayName { get; set; }
        public string Url { get; set; }
        public string Icon { get; set; }
        public int Order { get; set; }
    }
}
