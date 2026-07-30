using Application.Promotions.Dtos;
using Domain.Entities;

namespace Application.Promotions
{
    public static class PromotionMapper
    {
        public static StudentPromotionDto ToDto(StudentPromotion promotion)
        {
            var promotionDto = new StudentPromotionDto
            {
                Id = promotion.Id,
                StudentId = promotion.StudentId,
                StudentName = promotion.Student != null ? (promotion.Student.FirstName + " " + promotion.Student.LastName) : null,
                AdmissionNo = promotion.Student != null ? promotion.Student.AdmissionNo : null,
                FromEnrollmentId = promotion.FromEnrollmentId,
                FromGradeCode = promotion.FromEnrollment != null && promotion.FromEnrollment.ClassSection != null && promotion.FromEnrollment.ClassSection.AcademicClass != null ? promotion.FromEnrollment.ClassSection.AcademicClass.GradeCode : null,
                FromSectionCode = promotion.FromEnrollment != null && promotion.FromEnrollment.ClassSection != null ? promotion.FromEnrollment.ClassSection.SectionCode : null,
                ToEnrollmentId = promotion.ToEnrollmentId,
                ToGradeCode = promotion.ToEnrollment != null && promotion.ToEnrollment.ClassSection != null && promotion.ToEnrollment.ClassSection.AcademicClass != null ? promotion.ToEnrollment.ClassSection.AcademicClass.GradeCode : null,
                ToSectionCode = promotion.ToEnrollment != null && promotion.ToEnrollment.ClassSection != null ? promotion.ToEnrollment.ClassSection.SectionCode : null,
                PromotionDate = promotion.PromotionDate,
                PromotionType = promotion.PromotionType,
                Remarks = promotion.Remarks
            };

            return promotionDto;
        }
    }
}
