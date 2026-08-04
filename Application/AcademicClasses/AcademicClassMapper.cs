using Application.AcademicClasses.Dtos;
using Domain.Entities;
using Domain.Enums;

namespace Application.AcademicClasses
{
    public static class AcademicClassMapper
    {
        // Expects the class's Sections navigation to be loaded (the repository includes it).
        public static AcademicClassDto ToDto(AcademicClass academicClass)
        {
            var sectionDtos = new List<ClassSectionDto>();
            foreach (var section in academicClass.Sections.OrderBy(s => s.SectionCode))
            {
                var sectionDto = ToSectionDto(section);
                sectionDtos.Add(sectionDto);
            }

            var academicClassDto = new AcademicClassDto
            {
                Id = academicClass.Id,
                AcademicYearId = academicClass.AcademicYearId,
                GradeCode = academicClass.GradeCode,
                Order = academicClass.Order,
                Status = academicClass.Status,
                Sections = sectionDtos
            };

            return academicClassDto;
        }

        public static ClassSectionDto ToSectionDto(ClassSection classSection)
        {
            var classSectionDto = new ClassSectionDto
            {
                Id = classSection.Id,
                AcademicClassId = classSection.AcademicClassId,
                SectionCode = classSection.SectionCode,
                Capacity = classSection.Capacity,
                Status = classSection.Status
            };

            return classSectionDto;
        }

        // Expects the subject's ClassSection navigation to be loaded when section-scoped (the
        // repository includes it).
        public static ClassSubjectDto ToClassSubjectDto(ClassSubject classSubject)
        {
            var classSubjectDto = new ClassSubjectDto
            {
                Id = classSubject.Id,
                AcademicClassId = classSubject.AcademicClassId,
                SubjectCode = classSubject.SubjectCode,
                IsMandatory = classSubject.IsMandatory,
                DisplayOrder = classSubject.DisplayOrder,
                ClassSectionId = classSubject.ClassSectionId,
                SectionCode = classSubject.ClassSection != null ? classSubject.ClassSection.SectionCode : null,
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

        // Expects the assignment's Teacher->Employee, ClassSubject, ClassSection (when set), and
        // TimePeriod (when set) navigations to be loaded
        // (ITeacherRepository.GetAssignmentsByAcademicClassAsync includes them).
        public static ClassTeacherAssignmentDto ToTeacherAssignmentDto(TeacherAssignment assignment)
        {
            var teacher = assignment.Teacher;
            var employee = teacher != null ? teacher.Employee : null;

            var assignmentDto = new ClassTeacherAssignmentDto
            {
                Id = assignment.Id,
                TeacherId = assignment.TeacherId,
                TeacherName = employee != null ? BuildFullName(employee.FirstName, employee.MiddleName, employee.LastName) : null,
                EmployeeCode = employee != null ? employee.EmployeeCode : null,
                ClassSubjectId = assignment.ClassSubjectId,
                AcademicClassId = assignment.ClassSubject != null ? assignment.ClassSubject.AcademicClassId : Guid.Empty,
                SubjectCode = assignment.ClassSubject != null ? assignment.ClassSubject.SubjectCode : null,
                ClassSectionId = assignment.ClassSectionId,
                SectionCode = assignment.ClassSection != null ? assignment.ClassSection.SectionCode : null,
                Scope = assignment.ClassSectionId.HasValue ? SubjectScope.Section : SubjectScope.ClassWide,
                IsClassTeacher = assignment.IsClassTeacher,
                TimePeriodId = assignment.TimePeriodId,
                TimePeriodName = assignment.TimePeriod != null ? assignment.TimePeriod.Name : null
            };

            return assignmentDto;
        }

        // Small standalone helper (not shared with TeacherService/TeacherMapper's own copies --
        // mappers stay self-contained rather than reaching into another feature's class).
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
