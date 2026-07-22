using Domain.Enums;

namespace Domain.Entities
{
    // A single line on a SalarySlip. Amount is always positive; LineType carries the direction
    // (Earning adds to gross, Deduction/Tax/LoanEmi add to total deductions) and
    // SalaryLineSource the provenance. Hard-deleted, mutable only while the slip is Draft.
    // SalaryAdjustmentId/EmployeeLoanId are plain scalar lineage columns with NO database FK,
    // same snapshot reasoning as FeeInvoiceLine's lineage ids.
    public class SalarySlipLine : AuditableEntity
    {
        public Guid Id { get; set; }
        public Guid SalarySlipId { get; set; }
        public SalaryLineType LineType { get; set; }
        public SalaryLineSource Source { get; set; }
        public string ComponentCode { get; set; }
        public Guid? SalaryAdjustmentId { get; set; }
        public Guid? EmployeeLoanId { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }

        public virtual SalarySlip SalarySlip { get; set; }
    }
}
