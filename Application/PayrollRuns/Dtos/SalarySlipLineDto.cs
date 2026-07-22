using Domain.Enums;

namespace Application.PayrollRuns.Dtos
{
    public class SalarySlipLineDto
    {
        public Guid Id { get; set; }
        public SalaryLineType LineType { get; set; }
        public SalaryLineSource Source { get; set; }
        public string ComponentCode { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
    }
}
