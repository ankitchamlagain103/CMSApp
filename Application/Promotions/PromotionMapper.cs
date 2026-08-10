using Application.Common.Helpers;
using Application.Promotions.Dtos;
using Domain.Entities;

namespace Application.Promotions
{
    public static class PromotionMapper
    {
        // labelsByCode (2026-08-05): merged Grade+Section Config label map; null keeps every
        // Label field at its raw code.
        public static StudentPromotionDto ToDto(StudentPromotion promotion, IReadOnlyDictionary<string, string> labelsByCode = null)
        {
            var fromGradeCode = promotion.FromEnrollment != null && promotion.FromEnrollment.ClassSection != null && promotion.FromEnrollment.ClassSection.AcademicClass != null ? promotion.FromEnrollment.ClassSection.AcademicClass.GradeCode : null;
            var fromSectionCode = promotion.FromEnrollment != null && promotion.FromEnrollment.ClassSection != null ? promotion.FromEnrollment.ClassSection.SectionCode : null;
            var toGradeCode = promotion.ToEnrollment != null && promotion.ToEnrollment.ClassSection != null && promotion.ToEnrollment.ClassSection.AcademicClass != null ? promotion.ToEnrollment.ClassSection.AcademicClass.GradeCode : null;
            var toSectionCode = promotion.ToEnrollment != null && promotion.ToEnrollment.ClassSection != null ? promotion.ToEnrollment.ClassSection.SectionCode : null;

            var promotionDto = new StudentPromotionDto
            {
                Id = promotion.Id,
                StudentId = promotion.StudentId,
                StudentName = promotion.Student != null ? (promotion.Student.FirstName + " " + promotion.Student.LastName) : null,
                AdmissionNo = promotion.Student != null ? promotion.Student.AdmissionNo : null,
                FromEnrollmentId = promotion.FromEnrollmentId,
                FromGradeCode = fromGradeCode,
                FromGradeLabel = ConfigLabelHelper.Resolve(labelsByCode, fromGradeCode),
                FromSectionCode = fromSectionCode,
                FromSectionLabel = ConfigLabelHelper.Resolve(labelsByCode, fromSectionCode),
                ToEnrollmentId = promotion.ToEnrollmentId,
                ToGradeCode = toGradeCode,
                ToGradeLabel = ConfigLabelHelper.Resolve(labelsByCode, toGradeCode),
                ToSectionCode = toSectionCode,
                ToSectionLabel = ConfigLabelHelper.Resolve(labelsByCode, toSectionCode),
                PromotionDate = promotion.PromotionDate,
                PromotionType = promotion.PromotionType,
                Remarks = promotion.Remarks
            };

            return promotionDto;
        }
    }
}
