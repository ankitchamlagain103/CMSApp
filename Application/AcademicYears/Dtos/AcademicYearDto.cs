using Domain.Enums;

namespace Application.AcademicYears.Dtos
{
    public class AcademicYearDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsCurrent { get; set; }
        public RecordStatus Status { get; set; }
    }
}
