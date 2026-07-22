using Domain.Enums;

namespace Application.FeeInvoices.Dtos
{
    public class FeeInvoiceDto
    {
        public Guid Id { get; set; }
        public string InvoiceNo { get; set; }
        public Guid EnrollmentId { get; set; }
        public Guid AcademicYearId { get; set; }
        public Guid StudentId { get; set; }
        public string StudentName { get; set; }
        public string AdmissionNo { get; set; }
        public string GradeCode { get; set; }
        public string SectionCode { get; set; }
        public int BillingYear { get; set; }
        public int BillingMonth { get; set; }
        public FeeInvoiceStatus Status { get; set; }
        public decimal GrossAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal NetAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal BalanceAmount { get; set; }
        public decimal PreviousDueAmount { get; set; }
        public decimal CarriedForwardAmount { get; set; }
        public Guid? CarriedForwardToInvoiceId { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? GeneratedTs { get; set; }
        public string Remarks { get; set; }

        // Filled on detail/generation responses; left empty on the paged list.
        public List<FeeInvoiceLineDto> Lines { get; set; } = new List<FeeInvoiceLineDto>();
    }
}
