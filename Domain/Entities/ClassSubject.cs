namespace Domain.Entities
{
    // A subject offered by an AcademicClass. SubjectCode is a Config code (ConfigTypeCodes.Subject),
    // validated in the service layer, not a database FK. ClassSectionId scopes an OPTIONAL subject
    // to one section (null = offered to every section; mandatory subjects are always class-wide,
    // service-enforced). A subject appears either once class-wide or once per section, never both.
    // Hard-deleted (pure link row).
    public class ClassSubject : AuditableEntity
    {
        public Guid Id { get; set; }
        public Guid AcademicClassId { get; set; }
        public Guid? ClassSectionId { get; set; }
        public string SubjectCode { get; set; }
        public bool IsMandatory { get; set; }
        public int DisplayOrder { get; set; }

        // Grading metadata (2026-07-15) -- all nullable: existing rows predate this and a subject
        // may not have grading defined yet. Varies per class (a grade-appropriate marks scheme),
        // which is why these live here rather than on the global Subject Config catalog entry.
        public decimal? CreditHours { get; set; }
        public int? FullMarks { get; set; }
        public int? PassMarks { get; set; }
        public int? TheoryMarks { get; set; }
        public int? PracticalMarks { get; set; }

        public virtual AcademicClass AcademicClass { get; set; }
        public virtual ClassSection ClassSection { get; set; }
    }
}
