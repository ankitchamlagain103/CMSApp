using Application.Common.Helpers;
using Application.Employees.Dtos;
using Domain.Entities;
using Domain.Enums;

namespace Application.Employees
{
    // Moved here from the removed Application/Teachers/TeacherMapper.cs (2026-08-06, standalone
    // Teacher entity removed) -- dropped that file's ToDto(Teacher, ...) overload entirely, since
    // there's no separate Teacher profile to map anymore (EmployeeMapper.ToDto covers the whole
    // Employee row, teaching fields included).
    public static class TeacherAssignmentMapper
    {
        // Expects the assignment's ClassSubject -> AcademicClass -> AcademicYear chain (and
        // ClassSection, when set) to be loaded (GetAssignmentsAsync includes them).
        // classLabelsByCode merges Grade+Section+Subject -- see EmployeeService.LoadClassLabelMapAsync.
        public static TeacherServiceHistoryDto ToServiceHistoryDto(TeacherAssignment assignment, IReadOnlyDictionary<string, string> classLabelsByCode = null)
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
                GradeLabel = ConfigLabelHelper.Resolve(classLabelsByCode, academicClass.GradeCode),
                ClassSectionId = assignment.ClassSectionId,
                SectionCode = assignment.ClassSection != null ? assignment.ClassSection.SectionCode : null,
                SectionLabel = assignment.ClassSection != null ? ConfigLabelHelper.Resolve(classLabelsByCode, assignment.ClassSection.SectionCode) : null,
                Scope = assignment.ClassSectionId.HasValue ? SubjectScope.Section : SubjectScope.ClassWide,
                ClassSubjectId = assignment.ClassSubjectId,
                SubjectCode = classSubject.SubjectCode,
                SubjectLabel = ConfigLabelHelper.Resolve(classLabelsByCode, classSubject.SubjectCode),
                IsClassTeacher = assignment.IsClassTeacher
            };

            return historyDto;
        }

        // Expects the assignment's ClassSubject (and ClassSection/TimePeriod, when set)
        // navigations to be loaded (the repository includes them). classLabelsByCode as above.
        public static TeacherAssignmentDto ToAssignmentDto(TeacherAssignment assignment, IReadOnlyDictionary<string, string> classLabelsByCode = null)
        {
            var assignmentDto = new TeacherAssignmentDto
            {
                Id = assignment.Id,
                TeacherId = assignment.TeacherId,
                ClassSubjectId = assignment.ClassSubjectId,
                AcademicClassId = assignment.ClassSubject != null ? assignment.ClassSubject.AcademicClassId : Guid.Empty,
                SubjectCode = assignment.ClassSubject != null ? assignment.ClassSubject.SubjectCode : null,
                SubjectLabel = assignment.ClassSubject != null ? ConfigLabelHelper.Resolve(classLabelsByCode, assignment.ClassSubject.SubjectCode) : null,
                ClassSectionId = assignment.ClassSectionId,
                SectionCode = assignment.ClassSection != null ? assignment.ClassSection.SectionCode : null,
                SectionLabel = assignment.ClassSection != null ? ConfigLabelHelper.Resolve(classLabelsByCode, assignment.ClassSection.SectionCode) : null,
                Scope = assignment.ClassSectionId.HasValue ? SubjectScope.Section : SubjectScope.ClassWide,
                IsClassTeacher = assignment.IsClassTeacher,
                TimePeriodId = assignment.TimePeriodId,
                TimePeriodName = assignment.TimePeriod != null ? assignment.TimePeriod.Name : null,
                TimePeriodStartTime = assignment.TimePeriod != null ? assignment.TimePeriod.StartTime : (TimeSpan?)null,
                TimePeriodEndTime = assignment.TimePeriod != null ? assignment.TimePeriod.EndTime : (TimeSpan?)null
            };

            return assignmentDto;
        }
    }
}
