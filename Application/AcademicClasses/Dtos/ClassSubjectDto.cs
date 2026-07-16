using Domain.Enums;

namespace Application.AcademicClasses.Dtos
{
    // ClassSectionId/SectionCode are null when the subject is offered to every section of the
    // class; set only on section-scoped optional subjects. Scope is the same information as an
    // explicit named discriminator (ClassWide/Section) instead of requiring callers to infer it
    // from ClassSectionId's nullability.
    public class ClassSubjectDto
    {
        public Guid Id { get; set; }
        public Guid AcademicClassId { get; set; }
        public string SubjectCode { get; set; }
        public bool IsMandatory { get; set; }
        public int DisplayOrder { get; set; }
        public Guid? ClassSectionId { get; set; }
        public string SectionCode { get; set; }
        public SubjectScope Scope { get; set; }
        public decimal? CreditHours { get; set; }
        public int? FullMarks { get; set; }
        public int? PassMarks { get; set; }
        public int? TheoryMarks { get; set; }
        public int? PracticalMarks { get; set; }
    }
}
