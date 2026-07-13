namespace Application.AcademicClasses.Dtos
{
    // ClassSectionId/SectionCode are null when the subject is offered to every section of the
    // class; set only on section-scoped optional subjects.
    public class ClassSubjectDto
    {
        public Guid Id { get; set; }
        public Guid AcademicClassId { get; set; }
        public string SubjectCode { get; set; }
        public bool IsMandatory { get; set; }
        public int DisplayOrder { get; set; }
        public Guid? ClassSectionId { get; set; }
        public string SectionCode { get; set; }
    }
}
