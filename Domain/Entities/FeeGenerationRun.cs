namespace Domain.Entities
{
    // A period-keyed header recording that fee generation has run for one (AcademicYear,
    // BillingYear, BillingMonth) -- the fee-side counterpart of PayrollRun. Deliberately not an
    // FK-owned aggregate over FeeInvoice (unlike PayrollRun/SalarySlip): invoices for one period
    // can come from several scoped generate calls (one class at a time, or "all classes") and
    // also from FeePaymentService's advance billing, so the run is identified by the period
    // itself and its class/student/invoice breakdown is always read live from FeeInvoice rather
    // than stored. GenerateAsync finds-or-creates this row and stamps LastRegeneratedTs on every
    // later call for the same period. No Status/approve/pay lifecycle here -- that already lives
    // on each FeeInvoice.
    public class FeeGenerationRun : SoftDeleteAuditableEntity
    {
        public Guid Id { get; set; }
        public Guid AcademicYearId { get; set; }
        public int BillingYear { get; set; }
        public int BillingMonth { get; set; }
        public DateTime GeneratedTs { get; set; }
        public DateTime? LastRegeneratedTs { get; set; }
        public string Remarks { get; set; }

        public virtual AcademicYear AcademicYear { get; set; }
    }
}
