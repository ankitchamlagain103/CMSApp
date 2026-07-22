using Application.FeeInvoices.Dtos;

namespace Application.FeeGenerationRuns.Dtos
{
    public class FeeGenerationStudentGroupDto
    {
        public Guid EnrollmentId { get; set; }
        public Guid StudentId { get; set; }
        public string StudentName { get; set; }
        public string AdmissionNo { get; set; }
        public string SectionCode { get; set; }
        public decimal TotalNetAmount { get; set; }
        public decimal TotalPaidAmount { get; set; }
        public decimal TotalOutstandingAmount { get; set; }
        public List<FeeInvoiceDto> Invoices { get; set; } = new List<FeeInvoiceDto>();
    }
}
