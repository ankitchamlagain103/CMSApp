using Domain.Enums;

namespace Application.TimePeriods.Commands
{
    public class UpdateTimePeriodCommand
    {
        public string Name { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public PeriodKind Kind { get; set; }
        public int Order { get; set; }
    }
}
