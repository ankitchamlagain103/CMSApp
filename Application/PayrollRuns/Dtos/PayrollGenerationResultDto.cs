namespace Application.PayrollRuns.Dtos
{
    // What a payroll run generation did -- the created Draft run plus every employee skipped,
    // with the reason (no compensation plan, no tax slabs for their assessment type, ...).
    public class PayrollGenerationResultDto
    {
        public PayrollRunDto Run { get; set; }
        public List<PayrollSkipDto> Skipped { get; set; } = new List<PayrollSkipDto>();
    }
}
