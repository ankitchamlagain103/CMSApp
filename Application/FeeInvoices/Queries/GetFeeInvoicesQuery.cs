using Domain.Enums;

namespace Application.FeeInvoices.Queries
{
    public class GetFeeInvoicesQuery
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public Guid? AcademicYearId { get; set; }
        public int? BillingYear { get; set; }
        public int? BillingMonth { get; set; }
        public Guid? AcademicClassId { get; set; }
        public Guid? ClassSectionId { get; set; }
        public Guid? EnrollmentId { get; set; }

        // Accepts either repeated keys (?status=4&status=6) or one comma-separated value
        // (?status=4,6) -- ASP.NET Core's default query binder supports both for a List<T>. A
        // single ?status=4 keeps working exactly as before, binding to a one-item list.
        public List<FeeInvoiceStatus> Status { get; set; }

        // Matches the student's first/last name, admission no, or email (case-insensitive,
        // substring).
        public string Search { get; set; }
    }
}
