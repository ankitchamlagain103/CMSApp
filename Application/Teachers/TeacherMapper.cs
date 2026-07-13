using Application.Teachers.Dtos;
using Domain.Entities;

namespace Application.Teachers
{
    public static class TeacherMapper
    {
        public static TeacherDto ToDto(Teacher teacher)
        {
            var teacherDto = new TeacherDto
            {
                Id = teacher.Id,
                EmployeeNo = teacher.EmployeeNo,
                FirstName = teacher.FirstName,
                MiddleName = teacher.MiddleName,
                LastName = teacher.LastName,
                Email = teacher.Email,
                Phone = teacher.Phone,
                JoiningDate = teacher.JoiningDate,
                Status = teacher.Status
            };

            return teacherDto;
        }

        public static TeacherQualificationDto ToQualificationDto(TeacherQualification qualification)
        {
            var qualificationDto = new TeacherQualificationDto
            {
                Id = qualification.Id,
                TeacherId = qualification.TeacherId,
                QualificationCode = qualification.QualificationCode,
                CourseName = qualification.CourseName,
                Institution = qualification.Institution,
                CompletionYear = qualification.CompletionYear,
                Score = qualification.Score,
                Remarks = qualification.Remarks
            };

            return qualificationDto;
        }

        public static TeacherDocumentDto ToDocumentDto(TeacherDocument document)
        {
            var documentDto = new TeacherDocumentDto
            {
                Id = document.Id,
                TeacherId = document.TeacherId,
                DocumentTypeCode = document.DocumentTypeCode,
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

        // Expects the assignment's ClassSubject (and ClassSection, when set) navigations to be
        // loaded (the repository includes them).
        public static TeacherAssignmentDto ToAssignmentDto(TeacherAssignment assignment)
        {
            var assignmentDto = new TeacherAssignmentDto
            {
                Id = assignment.Id,
                TeacherId = assignment.TeacherId,
                ClassSubjectId = assignment.ClassSubjectId,
                AcademicClassId = assignment.ClassSubject != null ? assignment.ClassSubject.AcademicClassId : Guid.Empty,
                SubjectCode = assignment.ClassSubject != null ? assignment.ClassSubject.SubjectCode : null,
                ClassSectionId = assignment.ClassSectionId,
                SectionCode = assignment.ClassSection != null ? assignment.ClassSection.SectionCode : null,
                IsClassTeacher = assignment.IsClassTeacher
            };

            return assignmentDto;
        }
    }
}
