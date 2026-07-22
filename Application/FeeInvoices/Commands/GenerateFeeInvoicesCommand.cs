namespace Application.FeeInvoices.Commands
{
    // One generation run: every Enrolled enrollment of the academic year (optionally narrowed
    // to one class, or one section of it) gets a Draft invoice for the billing month.
    // AcademicClassId is the "this grade, all sections" scope -- without it a UI class picker
    // silently fell back to the whole year (the Nursery-vs-LKG generation bug). Idempotent --
    // enrollments whose month is already invoiced are skipped and reported;
    // RegenerateDrafts = true replaces invoices still in Draft (picking up configuration
    // changes) but never touches anything past Draft.
    public class GenerateFeeInvoicesCommand
    {
        public Guid AcademicYearId { get; set; }
        public int BillingYear { get; set; }
        public int BillingMonth { get; set; }
        public Guid? AcademicClassId { get; set; }
        public Guid? ClassSectionId { get; set; }
        public bool RegenerateDrafts { get; set; }
    }
}
