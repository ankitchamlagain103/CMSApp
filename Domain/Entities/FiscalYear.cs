using Domain.Enums;

namespace Domain.Entities
{
    // Nepal's government fiscal year (roughly mid-Shrawan to mid-Ashad) does not align with this
    // school's AcademicYear (see AcademicYear's own date convention) -- kept as a distinct entity
    // so payroll/tax stays correctly scoped to the real tax year regardless of how the school
    // defines its academic year. Same single-IsCurrent invariant as AcademicYear (service demotes
    // others on promote).
    public class FiscalYear : SoftDeleteAuditableEntity
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsCurrent { get; set; }
        public RecordStatus Status { get; set; }

        // The configurable "C" in Nepal's retirement-fund "least of three" tax exemption rule
        // (500,000 in the reference payslip example, but this changes by budget -- same
        // "configurable, not hardcoded" reasoning as TaxSlab itself).
        public decimal RetirementExemptionCapAmount { get; set; }

        public virtual ICollection<TaxSlab> TaxSlabs { get; set; } = new List<TaxSlab>();
    }
}
