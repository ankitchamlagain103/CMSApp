using Domain.Enums;

namespace Domain.Entities
{
    // One monthly payroll execution: a header owning one SalarySlip per employee for a fiscal
    // month (MonthIndex 1-12, Shrawan..Ashad -- the same fiscal-month convention as
    // MonthlyBreakdownCalculator). At most one live (non-Cancelled) run exists per
    // (FiscalYearId, MonthIndex). ApprovedBy is stamped from ICurrentUserService at approval,
    // separate from the generic audit columns, because approval is a domain event, not a row
    // edit.
    public class PayrollRun : SoftDeleteAuditableEntity
    {
        public Guid Id { get; set; }
        public Guid FiscalYearId { get; set; }
        public int MonthIndex { get; set; }
        public PayrollRunStatus Status { get; set; }
        public DateTime? GeneratedTs { get; set; }
        public DateTime? ApprovedTs { get; set; }
        public string ApprovedBy { get; set; }
        public DateTime? PaidTs { get; set; }
        public string Remarks { get; set; }

        public virtual FiscalYear FiscalYear { get; set; }
        public virtual ICollection<SalarySlip> Slips { get; set; } = new List<SalarySlip>();
    }
}
