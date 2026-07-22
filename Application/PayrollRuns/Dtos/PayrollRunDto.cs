using Domain.Enums;

namespace Application.PayrollRuns.Dtos
{
    public class PayrollRunDto
    {
        public Guid Id { get; set; }
        public Guid FiscalYearId { get; set; }
        public string FiscalYearCode { get; set; }
        public int MonthIndex { get; set; }
        public PayrollRunStatus Status { get; set; }
        public DateTime? GeneratedTs { get; set; }
        public DateTime? ApprovedTs { get; set; }
        public string ApprovedBy { get; set; }
        public DateTime? PaidTs { get; set; }
        public string Remarks { get; set; }
        public int SlipCount { get; set; }
        public decimal TotalGrossEarnings { get; set; }
        public decimal TotalNetPay { get; set; }
        public string CreatedBy { get; set; }
        public DateTimeOffset CreatedTs { get; set; }
        public string UpdatedBy { get; set; }
        public DateTimeOffset? UpdatedTs { get; set; }

        // Slip summaries (no lines); filled on detail/generation responses, empty on the
        // paged run list.
        public List<SalarySlipDto> Slips { get; set; } = new List<SalarySlipDto>();
    }
}
