namespace Application.Dashboard.Dtos
{
    public class UpcomingHrEventDto
    {
        public Guid EmployeeId { get; set; }
        public string FullName { get; set; }
        public DateTime Date { get; set; }
    }
}
