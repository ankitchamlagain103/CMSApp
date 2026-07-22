using Domain.Enums;

namespace Application.PayrollRuns.Queries
{
    public class GetPayrollRunsQuery
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public Guid? FiscalYearId { get; set; }
        public PayrollRunStatus? Status { get; set; }
    }
}
