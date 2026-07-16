using Application.Enrollments.Dtos;
using Domain.Common;
using Domain.Entities;

namespace Application.Enrollments
{
    public static class EnrollmentMapper
    {
        // Expects the enrollment's Student navigation and ClassSection (with its AcademicClass)
        // navigation to be loaded (the repository includes them).
        public static EnrollmentDto ToDto(Enrollment enrollment)
        {
            var classSection = enrollment.ClassSection;
            var academicClass = classSection != null ? classSection.AcademicClass : null;

            var enrollmentDto = new EnrollmentDto
            {
                Id = enrollment.Id,
                StudentId = enrollment.StudentId,
                ClassSectionId = enrollment.ClassSectionId,
                AcademicClassId = classSection != null ? classSection.AcademicClassId : Guid.Empty,
                AcademicYearId = academicClass != null ? academicClass.AcademicYearId : Guid.Empty,
                GradeCode = academicClass != null ? academicClass.GradeCode : null,
                SectionCode = classSection != null ? classSection.SectionCode : null,
                RollNumber = enrollment.RollNumber,
                EnrollmentDate = enrollment.EnrollmentDate,
                Status = enrollment.Status,
                StudentAdmissionNo = enrollment.Student != null ? enrollment.Student.AdmissionNo : null,
                StudentFirstName = enrollment.Student != null ? enrollment.Student.FirstName : null,
                StudentLastName = enrollment.Student != null ? enrollment.Student.LastName : null
            };

            return enrollmentDto;
        }

        // Expects the elective's ClassSubject navigation to be loaded (the repository includes it).
        public static EnrollmentSubjectDto ToElectiveSubjectDto(EnrollmentSubject electiveSubject)
        {
            var electiveSubjectDto = new EnrollmentSubjectDto
            {
                Id = electiveSubject.Id,
                EnrollmentId = electiveSubject.EnrollmentId,
                ClassSubjectId = electiveSubject.ClassSubjectId,
                SubjectCode = electiveSubject.ClassSubject != null ? electiveSubject.ClassSubject.SubjectCode : null
            };

            return electiveSubjectDto;
        }

        public static StudentDiscountDto ToDiscountDto(StudentDiscount discount)
        {
            var discountDto = new StudentDiscountDto
            {
                Id = discount.Id,
                EnrollmentId = discount.EnrollmentId,
                DiscountTypeCode = discount.DiscountTypeCode,
                ValueType = discount.ValueType,
                Value = discount.Value,
                Remarks = discount.Remarks
            };

            return discountDto;
        }

        public static StudentScholarshipDto ToScholarshipDto(StudentScholarship scholarship)
        {
            var scholarshipDto = new StudentScholarshipDto
            {
                Id = scholarship.Id,
                EnrollmentId = scholarship.EnrollmentId,
                ScholarshipTypeCode = scholarship.ScholarshipTypeCode,
                ValueType = scholarship.ValueType,
                Value = scholarship.Value,
                Remarks = scholarship.Remarks
            };

            return scholarshipDto;
        }

        public static EnrollmentFeeSelectionDto ToFeeSelectionDto(EnrollmentFeeSelection feeSelection)
        {
            var feeSelectionDto = new EnrollmentFeeSelectionDto
            {
                Id = feeSelection.Id,
                EnrollmentId = feeSelection.EnrollmentId,
                FeeStructureItemId = feeSelection.FeeStructureItemId
            };

            return feeSelectionDto;
        }

        public static AwardSummaryDto ToAwardSummaryDto(AwardSummaryItem summaryItem)
        {
            var summaryDto = new AwardSummaryDto
            {
                TypeCode = summaryItem.TypeCode,
                StudentCount = summaryItem.StudentCount
            };

            return summaryDto;
        }
    }
}
