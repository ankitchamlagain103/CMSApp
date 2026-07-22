using Domain.Enums;

namespace Domain.Common.Filters
{
    public class PayrollRunFilter
    {
        public Guid? FiscalYearId { get; set; }
        public PayrollRunStatus? Status { get; set; }
    }
}
