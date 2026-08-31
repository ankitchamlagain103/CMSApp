namespace Application.TimePeriods.Dtos
{
    public class ClassTimePeriodMapResultDto
    {
        public List<ClassTimePeriodDto> Created { get; set; } = new List<ClassTimePeriodDto>();
        public List<ClassTimePeriodSkipDto> Skipped { get; set; } = new List<ClassTimePeriodSkipDto>();
    }
}
