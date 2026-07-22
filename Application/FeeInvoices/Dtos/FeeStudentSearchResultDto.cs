namespace Application.FeeInvoices.Dtos
{
    // One student match for the fee module's search box: the active enrollment (the id every
    // fee endpoint keys on) plus the live outstanding balance so a fee clerk can jump straight
    // to a statement or payment entry.
    public class FeeStudentSearchResultDto
    {
        public Guid EnrollmentId { get; set; }
        public Guid StudentId { get; set; }
        public string StudentName { get; set; }
        public string AdmissionNo { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public Guid AcademicYearId { get; set; }
        public string GradeCode { get; set; }
        public string SectionCode { get; set; }
        public decimal OutstandingAmount { get; set; }
    }
}
