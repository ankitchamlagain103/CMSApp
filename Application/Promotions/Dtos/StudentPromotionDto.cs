using Domain.Enums;

namespace Application.Promotions.Dtos
{
    // Student/grade/section fields are flattened from the enrollment chain, same convention as
    // EnrollmentDto/StudentResultDto -- a promotion history table renders with no per-row lookup.
    public class StudentPromotionDto
    {
        public Guid Id { get; set; }
        public Guid StudentId { get; set; }
        public string StudentName { get; set; }
        public string AdmissionNo { get; set; }
        public Guid FromEnrollmentId { get; set; }
        public string FromGradeCode { get; set; }
        public string FromGradeLabel { get; set; }
        public string FromSectionCode { get; set; }
        public string FromSectionLabel { get; set; }
        public Guid ToEnrollmentId { get; set; }
        public string ToGradeCode { get; set; }
        public string ToGradeLabel { get; set; }
        public string ToSectionCode { get; set; }
        public string ToSectionLabel { get; set; }
        public DateTime PromotionDate { get; set; }
        public PromotionType PromotionType { get; set; }
        public string Remarks { get; set; }
    }
}
