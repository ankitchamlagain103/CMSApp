using Domain.Enums;

namespace Domain.Entities
{
    // One employee's persisted payslip inside a PayrollRun -- the durable snapshot that
    // replaces the read-time-computed payslip once a month is generated. EmployeeSalaryId
    // records which compensation-plan revision was snapshotted; the slip's lines are the
    // record, so later plan edits never change an existing slip. PeriodStartDate/PeriodEndDate
    // copy the fiscal month's Gregorian window at generation time (used for loan-EMI due
    // checks and display). Totals are stored and re-derived on every Draft line edit.
    public class SalarySlip : SoftDeleteAuditableEntity
    {
        public Guid Id { get; set; }
        public string SlipNo { get; set; }
        public Guid PayrollRunId { get; set; }
        public Guid EmployeeId { get; set; }
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

        public virtual PayrollRun PayrollRun { get; set; }
        public virtual Employee Employee { get; set; }
        public virtual EmployeeSalary EmployeeSalary { get; set; }
        public virtual ICollection<SalarySlipLine> Lines { get; set; } = new List<SalarySlipLine>();
    }
}
