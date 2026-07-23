using Application.Teachers.Dtos;
using Domain.Entities;
using Domain.Enums;

namespace Application.Teachers
{
    public static class TeacherMapper
    {
        // Expects the teacher's Employee navigation to be loaded (the repository includes it).
        public static TeacherDto ToDto(Teacher teacher)
        {
            var employee = teacher.Employee;

            var teacherDto = new TeacherDto
            {
                Id = teacher.Id,
                EmployeeCode = employee != null ? employee.EmployeeCode : null,
                FirstName = employee != null ? employee.FirstName : null,
                MiddleName = employee != null ? employee.MiddleName : null,
                LastName = employee != null ? employee.LastName : null,
                Gender = employee != null ? employee.Gender : default,
                DateOfBirth = employee != null ? employee.DateOfBirth : null,
                Email = employee != null ? employee.Email : null,
                Phone = employee != null ? employee.Phone : null,
                JoinDate = employee != null ? employee.JoinDate : null,
                JobPositionCode = employee != null ? employee.JobPositionCode : null,
                Status = employee != null ? employee.EmploymentStatus : default,
                BankName = employee != null ? employee.BankName : null,
                BankAccountNumber = employee != null ? employee.BankAccountNumber : null,
                PaymentMode = employee != null ? employee.PaymentMode : default,
                TeachingLicenseNo = teacher.TeachingLicenseNo,
                ExperienceYears = teacher.ExperienceYears,
                Specialization = teacher.Specialization
            };

            return teacherDto;
        }

        // Expects the assignment's ClassSubject -> AcademicClass -> AcademicYear chain (and
        // ClassSection, when set) to be loaded (GetAssignmentsAsync includes them).
        public static TeacherServiceHistoryDto ToServiceHistoryDto(TeacherAssignment assignment)
        {
            var classSubject = assignment.ClassSubject;
            var academicClass = classSubject.AcademicClass;
            var academicYear = academicClass.AcademicYear;

            var historyDto = new TeacherServiceHistoryDto
            {
                AssignmentId = assignment.Id,
                AcademicYearId = academicYear.Id,
                AcademicYearCode = academicYear.Code,
                AcademicYearName = academicYear.Name,
                AcademicYearStartDate = academicYear.StartDate,
                AcademicClassId = academicClass.Id,
                GradeCode = academicClass.GradeCode,
                ClassSectionId = assignment.ClassSectionId,
                SectionCode = assignment.ClassSection != null ? assignment.ClassSection.SectionCode : null,
                Scope = assignment.ClassSectionId.HasValue ? SubjectScope.Section : SubjectScope.ClassWide,
                ClassSubjectId = assignment.ClassSubjectId,
                SubjectCode = classSubject.SubjectCode,
                IsClassTeacher = assignment.IsClassTeacher
            };

            return historyDto;
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
                Scope = assignment.ClassSectionId.HasValue ? SubjectScope.Section : SubjectScope.ClassWide,
                IsClassTeacher = assignment.IsClassTeacher
            };

            return assignmentDto;
        }
    }
}
