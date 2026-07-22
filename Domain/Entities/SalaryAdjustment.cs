using Domain.Enums;

namespace Domain.Entities
{
    // A pre-run monthly payroll override for one employee ("2 days unpaid leave in Magh",
    // "Dashain incentive this month") -- the middle tier between the recurring compensation
    // plan (EmployeeSalary line items) and manual edits on a Draft slip.
    // AdjustmentTypeCode is a Config code (ConfigTypeCodes.SalaryAdjustmentType), validated in
    // the service layer, not a database FK; UNPAID_LEAVE (Domain/Constants/
    // SalaryAdjustmentTypeCodes) gets special day-count handling via Quantity. Percentage
    // values resolve against the salary revision's BASIC component, same convention as
    // EmployeeSalaryComponent. Pending rows are consumed by the payroll run of their
    // (FiscalYearId, MonthIndex) and stamped Applied + AppliedSalarySlipId; cancelling a Draft
    // run re-pends them. Soft-deleted (financial-audit record).
    public class SalaryAdjustment : SoftDeleteAuditableEntity
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public Guid FiscalYearId { get; set; }
        public int MonthIndex { get; set; }
        public string AdjustmentTypeCode { get; set; }
        public AdjustmentDirection Direction { get; set; }
        public AwardValueType ValueType { get; set; }
        public decimal Value { get; set; }

        // Multiplier for day/occurrence-based adjustments (days of unpaid leave, number of
        // late arrivals); null for flat one-shot amounts.
        public decimal? Quantity { get; set; }

        public string Remarks { get; set; }
        public AdjustmentStatus Status { get; set; }
        public Guid? AppliedSalarySlipId { get; set; }

        public virtual Employee Employee { get; set; }
        public virtual FiscalYear FiscalYear { get; set; }
    }
}
