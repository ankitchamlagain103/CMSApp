using Application.AcademicClasses.Dtos;
using Application.Common.Helpers;
using Domain.Entities;
using Domain.Enums;

namespace Application.AcademicClasses
{
    public static class AcademicClassMapper
    {
        // Expects the class's Sections navigation to be loaded (the repository includes it).
        // classLabelsByCode (2026-08-05) resolves Grade/Section/Subject codes server-side -- see
        // AcademicClassService.LoadClassLabelMapAsync; null/omitted leaves every XxxLabel field null.
        public static AcademicClassDto ToDto(AcademicClass academicClass, IReadOnlyDictionary<string, string> classLabelsByCode = null)
        {
            var sectionDtos = new List<ClassSectionDto>();
            foreach (var section in academicClass.Sections.OrderBy(s => s.SectionCode))
            {
                var sectionDto = ToSectionDto(section, classLabelsByCode);
                sectionDtos.Add(sectionDto);
            }

            var academicClassDto = new AcademicClassDto
            {
                Id = academicClass.Id,
                AcademicYearId = academicClass.AcademicYearId,
                GradeCode = academicClass.GradeCode,
                GradeLabel = ConfigLabelHelper.Resolve(classLabelsByCode, academicClass.GradeCode),
                Order = academicClass.Order,
                Status = academicClass.Status,
                Sections = sectionDtos
            };

            return academicClassDto;
        }

        public static ClassSectionDto ToSectionDto(ClassSection classSection, IReadOnlyDictionary<string, string> classLabelsByCode = null)
        {
            var classSectionDto = new ClassSectionDto
            {
                Id = classSection.Id,
                AcademicClassId = classSection.AcademicClassId,
                SectionCode = classSection.SectionCode,
                SectionLabel = ConfigLabelHelper.Resolve(classLabelsByCode, classSection.SectionCode),
                Capacity = classSection.Capacity,
                Status = classSection.Status
            };

            return classSectionDto;
        }

        // Expects the subject's ClassSection navigation to be loaded when section-scoped (the
        // repository includes it).
        public static ClassSubjectDto ToClassSubjectDto(ClassSubject classSubject, IReadOnlyDictionary<string, string> classLabelsByCode = null)
        {
            var classSubjectDto = new ClassSubjectDto
            {
                Id = classSubject.Id,
                AcademicClassId = classSubject.AcademicClassId,
                SubjectCode = classSubject.SubjectCode,
                SubjectLabel = ConfigLabelHelper.Resolve(classLabelsByCode, classSubject.SubjectCode),
                IsMandatory = classSubject.IsMandatory,
                DisplayOrder = classSubject.DisplayOrder,
                ClassSectionId = classSubject.ClassSectionId,
                SectionCode = classSubject.ClassSection != null ? classSubject.ClassSection.SectionCode : null,
                SectionLabel = classSubject.ClassSection != null ? ConfigLabelHelper.Resolve(classLabelsByCode, classSubject.ClassSection.SectionCode) : null,
                Scope = classSubject.ClassSectionId.HasValue ? SubjectScope.Section : SubjectScope.ClassWide,
                CreditHours = classSubject.CreditHours,
                FullMarks = classSubject.FullMarks,
                PassMarks = classSubject.PassMarks,
                TheoryMarks = classSubject.TheoryMarks,
                PracticalMarks = classSubject.PracticalMarks,
                HasTheory = classSubject.HasTheory,
                HasPractical = classSubject.HasPractical,
                TheoryPassMarks = classSubject.TheoryPassMarks,
                PracticalPassMarks = classSubject.PracticalPassMarks
            };

            return classSubjectDto;
        }

        // Expects the assignment's Employee, ClassSubject, ClassSection (when set), and
        // TimePeriod (when set) navigations to be loaded
        // (IEmployeeRepository.GetAssignmentsByAcademicClassAsync includes them).
        public static ClassTeacherAssignmentDto ToTeacherAssignmentDto(TeacherAssignment assignment, IReadOnlyDictionary<string, string> classLabelsByCode = null)
        {
            var employee = assignment.Employee;

            var assignmentDto = new ClassTeacherAssignmentDto
            {
                Id = assignment.Id,
                TeacherId = assignment.TeacherId,
                TeacherName = employee != null ? BuildFullName(employee.FirstName, employee.MiddleName, employee.LastName) : null,
                EmployeeCode = employee != null ? employee.EmployeeCode : null,
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

        // Small standalone helper (not shared with EmployeeMapper/TeacherAssignmentMapper's own
        // copies -- mappers stay self-contained rather than reaching into another feature's class).
        private static string BuildFullName(string firstName, string middleName, string lastName)
        {
            var nameParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(firstName))
            {
                nameParts.Add(firstName);
            }

            if (!string.IsNullOrWhiteSpace(middleName))
            {
                nameParts.Add(middleName);
            }

            if (!string.IsNullOrWhiteSpace(lastName))
            {
                nameParts.Add(lastName);
            }

            var fullName = string.Join(" ", nameParts);
            return fullName;
        }
    }
}
