namespace Application.FeeInvoices.Dtos
{
    // The ledger-style Statement of Account for one enrollment: chronological entries (invoice
    // debits, payment credits) with a running balance. ClosingBalance is the live outstanding
    // amount -- the same debits-minus-credits number the last entry's Balance shows. Draft and
    // Cancelled invoices and Voided payments never appear (nothing is owed or received on
    // them).
    public class FeeAccountStatementDto
    {
        public Guid EnrollmentId { get; set; }
        public Guid StudentId { get; set; }
        public string StudentName { get; set; }
        public string AdmissionNo { get; set; }
        public string Email { get; set; }
        public string GradeCode { get; set; }
        public string SectionCode { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public decimal ClosingBalance { get; set; }
        public List<FeeAccountEntryDto> Entries { get; set; } = new List<FeeAccountEntryDto>();
    }
}
