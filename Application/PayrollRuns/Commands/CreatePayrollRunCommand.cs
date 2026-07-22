namespace Application.PayrollRuns.Commands
{
    public class CreatePayrollRunCommand
    {
        public Guid FiscalYearId { get; set; }
        public int MonthIndex { get; set; }
        public string Remarks { get; set; }
    }
}
