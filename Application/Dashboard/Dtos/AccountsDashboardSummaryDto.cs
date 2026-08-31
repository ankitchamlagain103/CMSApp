namespace Application.Dashboard.Dtos
{
    // Composite "Accounts" (finance) dashboard widget (2026-07-28) -- one call covering fee
    // collection, outstanding dues, and the current payroll run, the same one-call-per-persona
    // shape as DashboardSummaryDto.
    public class AccountsDashboardSummaryDto
    {
        public decimal FeeCollectedToday { get; set; }
        public decimal FeeCollectedThisMonth { get; set; }
        public decimal FeeCollectedThisFiscalYear { get; set; }
        public decimal TotalOutstandingDue { get; set; }
        public List<FeeInvoiceStatusCountDto> InvoiceCountsByStatus { get; set; } = new List<FeeInvoiceStatusCountDto>();
        public int PendingFeeAdjustmentCount { get; set; }

        // Null when no PayrollRun exists yet.
        public CurrentPayrollRunSummaryDto CurrentPayrollRun { get; set; }
        public List<RecentFeePaymentDto> RecentPayments { get; set; } = new List<RecentFeePaymentDto>();
    }
}
