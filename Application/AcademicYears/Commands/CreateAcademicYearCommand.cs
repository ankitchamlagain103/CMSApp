namespace Application.AcademicYears.Commands
{
    public class CreateAcademicYearCommand
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsCurrent { get; set; }
    }
}
