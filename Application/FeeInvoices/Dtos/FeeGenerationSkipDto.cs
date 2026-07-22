namespace Application.FeeInvoices.Dtos
{
    public class FeeGenerationSkipDto
    {
        public Guid EnrollmentId { get; set; }
        public string StudentName { get; set; }
        public string Reason { get; set; }
    }
}
