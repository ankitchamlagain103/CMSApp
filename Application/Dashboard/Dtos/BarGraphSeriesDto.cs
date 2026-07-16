namespace Application.Dashboard.Dtos
{
    public class BarGraphSeriesDto
    {
        public string Name { get; set; }
        public List<int> Data { get; set; } = new List<int>();
    }
}
