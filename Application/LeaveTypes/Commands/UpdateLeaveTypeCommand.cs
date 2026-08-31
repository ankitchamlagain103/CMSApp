namespace Application.LeaveTypes.Commands
{
    public class UpdateLeaveTypeCommand
    {
        public string Name { get; set; }
        public decimal DaysPerYear { get; set; }
        public bool CarryForward { get; set; }
        public bool IsPaid { get; set; }

        // Policy configuration (2026-07-24) -- see CreateLeaveTypeCommand's copies.
        public int? MaxConsecutiveDays { get; set; }
        public decimal? MaxDaysPerWeek { get; set; }
        public decimal? MaxDaysPerMonth { get; set; }
    }
}
