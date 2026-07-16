namespace Application.Enrollments.Dtos
{
    // The full priced fee view for one enrollment -- bound to its academic year through the
    // enrollment's own class/section chain, so no separate year parameter is needed. Meant for
    // the student detail page.
    public class EnrollmentFeeStructureDto
    {
        public Guid EnrollmentId { get; set; }
        public Guid AcademicYearId { get; set; }
        public Guid AcademicClassId { get; set; }
        public string GradeCode { get; set; }
        public List<FeeLineItemDto> FeeItems { get; set; } = new List<FeeLineItemDto>();
        public List<StudentDiscountDto> Discounts { get; set; } = new List<StudentDiscountDto>();
        public List<StudentScholarshipDto> Scholarships { get; set; } = new List<StudentScholarshipDto>();
        public FeeStructureSummaryDto Summary { get; set; }
    }
}
