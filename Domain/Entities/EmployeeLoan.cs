using Domain.Enums;

namespace Domain.Entities
{
    // A requested/approved loan or advance against future salary. Financial-audit record, so
    // soft-deleted like StudentDiscount/StudentScholarship rather than hard-deleted like the pure
    // salary line items. LoanTypeCode is a Config code (ConfigTypeCodes.DeductionType), restricted
    // to Domain/Constants/LoanTypeCodes (LOAN/ADVANCE) rather than any DeductionType code.
    //
    // Repayment progress is deliberately NOT a separate ledger table -- it's computed from
    // StartDate/EmiAmount/PrincipalAmount against the current date wherever it's needed
    // (EmployeeMapper/EmployeeService), and the EMI is folded into the Payslip/Tax-Details monthly
    // breakdown at read time for any month on/after StartDate while Status == Approved and the
    // loan isn't yet fully repaid. This means a later salary revision (a raise) needs no special
    // handling to "carry forward" the deduction -- the loan lives independently of any
    // EmployeeSalary row.
    public class EmployeeLoan : SoftDeleteAuditableEntity
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public string LoanTypeCode { get; set; }
        public decimal PrincipalAmount { get; set; }
        public decimal EmiAmount { get; set; }
        public DateTime RequestedDate { get; set; }
        public DateTime StartDate { get; set; }
        public LoanStatus Status { get; set; }
        public string Remarks { get; set; }

        public virtual Employee Employee { get; set; }
    }
}
