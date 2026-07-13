namespace Application.Teachers.Queries
{
    public class GetTeachersQuery
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string Search { get; set; }
    }
}
