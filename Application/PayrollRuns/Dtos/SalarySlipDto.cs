using Domain.Enums;

namespace Application.PayrollRuns.Dtos
{
    public class SalarySlipDto
    {
        public Guid Id { get; set; }
        public string SlipNo { get; set; }
        public Guid PayrollRunId { get; set; }
        public Guid EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public string EmployeeCode { get; set; }
        public Guid EmployeeSalaryId { get; set; }
        public SalarySlipStatus Status { get; set; }
        public DateTime PeriodStartDate { get; set; }
        public DateTime PeriodEndDate { get; set; }
        public int MonthDays { get; set; }
        public decimal PayDays { get; set; }
        public decimal UnpaidLeaveDays { get; set; }
        public decimal GrossEarnings { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal NetPay { get; set; }
        public string Remarks { get; set; }

        // Filled on the slip detail; left empty in run-level summaries.
        public List<SalarySlipLineDto> Lines { get; set; } = new List<SalarySlipLineDto>();
    }
}
