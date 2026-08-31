namespace Application.LeaveTypes.Commands
{
    public class CreateLeaveTypeCommand
    {
        public string Name { get; set; }
        public decimal DaysPerYear { get; set; }
        public bool CarryForward { get; set; }
        public bool IsPaid { get; set; } = true;

        // Policy configuration (2026-07-24) -- both optional, see the doc comment on
        // Domain/Entities/LeaveType for what each one means.
        public int? MaxConsecutiveDays { get; set; }
        public decimal? MaxDaysPerWeek { get; set; }
        public decimal? MaxDaysPerMonth { get; set; }
    }
}
