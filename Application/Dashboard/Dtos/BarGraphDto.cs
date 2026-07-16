namespace Application.Dashboard.Dtos
{
    public class BarGraphDto
    {
        public string Metric { get; set; }
        public string Title { get; set; }
        public List<string> Labels { get; set; } = new List<string>();
        public List<BarGraphSeriesDto> Series { get; set; } = new List<BarGraphSeriesDto>();
    }
}
