using Application.Common.Helpers;
using Application.Students.Dtos;
using Domain.Entities;

namespace Application.Students
{
    public static class StudentMapper
    {
        public static StudentDto ToDto(Student student)
        {
            var studentDto = new StudentDto
            {
                Id = student.Id,
                UserId = student.UserId,
                AdmissionNo = student.AdmissionNo,
                FirstName = student.FirstName,
                MiddleName = student.MiddleName,
                LastName = student.LastName,
                Gender = student.Gender,
                DateOfBirth = student.DateOfBirth,
                Email = student.Email,
                Phone = student.Phone,
                Address = student.Address,
                AdmissionDate = student.AdmissionDate,
                Status = student.Status,
                CreatedBy = student.CreatedBy,
                CreatedTs = student.CreatedTs,
                UpdatedBy = student.UpdatedBy,
                UpdatedTs = student.UpdatedTs
            };

            return studentDto;
        }

        // Detail-shape overload: same as above plus the guardian links (each with its Guardian
        // navigation loaded) flattened into the DTO. Only CreateStudentAsync/UpdateStudentAsync
        // call this overload as of 2026-08-05 -- GetStudentByIdAsync uses the plain ToDto(student)
        // above, since the Guardians tab has its own dedicated endpoint now.
        public static StudentDto ToDto(Student student, IReadOnlyList<StudentGuardian> guardianLinks, IReadOnlyDictionary<string, string> relationshipLabelsByCode)
        {
            var studentDto = ToDto(student);

            var guardianDtos = new List<StudentGuardianDto>();
            foreach (var guardianLink in guardianLinks)
            {
                var guardianDto = ToGuardianLinkDto(guardianLink, relationshipLabelsByCode);
                guardianDtos.Add(guardianDto);
            }

            studentDto.Guardians = guardianDtos;
            return studentDto;
        }

        public static StudentDocumentDto ToDocumentDto(StudentDocument document, IReadOnlyDictionary<string, string> labelsByCode = null)
        {
            var documentDto = new StudentDocumentDto
            {
                Id = document.Id,
                StudentId = document.StudentId,
                DocumentTypeCode = document.DocumentTypeCode,
                DocumentTypeLabel = ConfigLabelHelper.Resolve(labelsByCode, document.DocumentTypeCode),
                DocumentName = document.DocumentName,
                FileName = document.FileName,
                ContentType = document.ContentType,
                FileSizeBytes = document.FileSizeBytes,
                ValidUntil = document.ValidUntil,
                Remarks = document.Remarks,
                UploadedTs = document.CreatedTs
            };

            return documentDto;
        }

        // Expects the enrollment's ClassSection -> AcademicClass -> AcademicYear chain to be
        // loaded (the history repository query includes it).
        public static StudentEnrollmentHistoryDto ToEnrollmentHistoryDto(Enrollment enrollment, IReadOnlyDictionary<string, string> classLabelsByCode)
        {
            var classSection = enrollment.ClassSection;
            var academicClass = classSection.AcademicClass;
            var academicYear = academicClass.AcademicYear;

            var historyDto = new StudentEnrollmentHistoryDto
            {
                EnrollmentId = enrollment.Id,
                AcademicYearId = academicYear.Id,
                AcademicYearCode = academicYear.Code,
                AcademicYearName = academicYear.Name,
                AcademicYearStartDate = academicYear.StartDate,
                AcademicClassId = academicClass.Id,
                GradeCode = academicClass.GradeCode,
                GradeLabel = ConfigLabelHelper.Resolve(classLabelsByCode, academicClass.GradeCode),
                ClassSectionId = classSection.Id,
                SectionCode = classSection.SectionCode,
                SectionLabel = ConfigLabelHelper.Resolve(classLabelsByCode, classSection.SectionCode),
                RollNumber = enrollment.RollNumber,
                EnrollmentDate = enrollment.EnrollmentDate,
                Status = enrollment.Status
            };

            return historyDto;
        }

        // Expects the link's Guardian navigation to be loaded (the repository includes it).
        public static StudentGuardianDto ToGuardianLinkDto(StudentGuardian link, IReadOnlyDictionary<string, string> relationshipLabelsByCode)
        {
            var studentGuardianDto = new StudentGuardianDto
            {
                Id = link.Id,
                StudentId = link.StudentId,
                GuardianId = link.GuardianId,
                RelationshipCode = link.RelationshipCode,
                RelationshipLabel = ConfigLabelHelper.Resolve(relationshipLabelsByCode, link.RelationshipCode),
                IsPrimary = link.IsPrimary,
                GuardianFirstName = link.Guardian != null ? link.Guardian.FirstName : null,
                GuardianLastName = link.Guardian != null ? link.Guardian.LastName : null,
                GuardianPhone = link.Guardian != null ? link.Guardian.Phone : null,
                GuardianEmail = link.Guardian != null ? link.Guardian.Email : null
            };

            return studentGuardianDto;
        }
    }
}
