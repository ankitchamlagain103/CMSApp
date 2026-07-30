namespace Application.LeaveTypes.Dtos
{
    public class LeaveTypeDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public decimal DaysPerYear { get; set; }
        public bool CarryForward { get; set; }
        public bool IsPaid { get; set; }

        public int? MaxConsecutiveDays { get; set; }
        public decimal? MaxDaysPerWeek { get; set; }
        public decimal? MaxDaysPerMonth { get; set; }
    }
}
