namespace Application.FeeInvoices.Queries
{
    // Student search for the fee module: Search matches first/last name, admission no, or
    // email (case-insensitive, substring) across active (Enrolled) enrollments, optionally
    // narrowed to one academic year.
    public class SearchFeeStudentsQuery
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public Guid? AcademicYearId { get; set; }
        public string Search { get; set; }
    }
}
