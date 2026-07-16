using Domain.Enums;

namespace Application.Payroll.FiscalYears.Commands
{
    // Code is immutable -- not in the body.
    public class UpdateFiscalYearCommand
    {
        public string Name { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsCurrent { get; set; }
        public RecordStatus Status { get; set; }
    }
}
