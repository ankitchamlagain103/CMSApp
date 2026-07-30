using Domain.Enums;

namespace Application.Dashboard.Dtos
{
    public class CurrentPayrollRunSummaryDto
    {
        public Guid PayrollRunId { get; set; }
        public string FiscalYearCode { get; set; }
        public int MonthIndex { get; set; }
        public PayrollRunStatus Status { get; set; }

        // Excludes Cancelled slips -- same aggregate rule PayrollRunMapper.ToDto already applies.
        public int SlipCount { get; set; }
        public decimal TotalNetPay { get; set; }
    }
}
