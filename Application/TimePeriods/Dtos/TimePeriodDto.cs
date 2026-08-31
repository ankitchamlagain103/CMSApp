using Domain.Enums;

namespace Application.TimePeriods.Dtos
{
    public class TimePeriodDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public PeriodKind Kind { get; set; }
        public int Order { get; set; }
    }
}
