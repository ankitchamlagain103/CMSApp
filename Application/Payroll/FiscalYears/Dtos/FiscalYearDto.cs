using Domain.Enums;

namespace Application.Payroll.FiscalYears.Dtos
{
    public class FiscalYearDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsCurrent { get; set; }
        public RecordStatus Status { get; set; }
        public decimal RetirementExemptionCapAmount { get; set; }
    }
}
