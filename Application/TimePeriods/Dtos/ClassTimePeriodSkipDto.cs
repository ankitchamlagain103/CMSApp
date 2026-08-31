namespace Application.TimePeriods.Dtos
{
    public class ClassTimePeriodSkipDto
    {
        public Guid AcademicClassId { get; set; }
        public Guid TimePeriodId { get; set; }
        public string Reason { get; set; }
    }
}
