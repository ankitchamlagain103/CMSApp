namespace Application.Payroll.FiscalYears.Commands
{
    public class CreateFiscalYearCommand
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsCurrent { get; set; }
        public decimal RetirementExemptionCapAmount { get; set; }
    }
}
