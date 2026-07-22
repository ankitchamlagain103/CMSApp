using Domain.Enums;

namespace Domain.Common.Filters
{
    public class FeeInvoiceFilter
    {
        public Guid? AcademicYearId { get; set; }
        public int? BillingYear { get; set; }
        public int? BillingMonth { get; set; }
        public Guid? AcademicClassId { get; set; }
        public Guid? ClassSectionId { get; set; }
        public Guid? EnrollmentId { get; set; }

        // Multiple values are OR'd together (Contains) -- a single value keeps behaving exactly
        // as the old FeeInvoiceStatus? equality check did.
        public List<FeeInvoiceStatus> Status { get; set; }
        public string Search { get; set; }
    }
}
