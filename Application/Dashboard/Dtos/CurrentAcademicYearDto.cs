namespace Application.Dashboard.Dtos
{
    public class CurrentAcademicYearDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalClasses { get; set; }
        public int TotalSections { get; set; }
        public int TotalActiveEnrollments { get; set; }
    }
}
