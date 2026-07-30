namespace Application.Dashboard.Dtos
{
    public class RecentHireDto
    {
        public Guid EmployeeId { get; set; }
        public string FullName { get; set; }
        public string JobPositionCode { get; set; }
        public string JobPositionLabel { get; set; }
        public DateTime JoinDate { get; set; }
    }
}
