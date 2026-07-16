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
                PracticalMarks = classSubject.PracticalMarks
            };

            return classSubjectDto;
        }
    }
}
