using Domain.Enums;

namespace Application.AcademicYears.Commands
{
    // Code is deliberately absent -- an academic year's code is its stable identifier once
    // referenced by classes/enrollments (same immutability reasoning as ConfigType.TypeCode).
    public class UpdateAcademicYearCommand
    {
        public string Name { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsCurrent { get; set; }
        public RecordStatus Status { get; set; }
    }
}
