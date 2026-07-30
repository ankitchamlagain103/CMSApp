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
        // FullMarks/PassMarks are the OVERALL totals; TheoryMarks/PracticalMarks are each
        // component's own full marks.
        public decimal? CreditHours { get; set; }
        public int? FullMarks { get; set; }
        public int? PassMarks { get; set; }
        public int? TheoryMarks { get; set; }
        public int? PracticalMarks { get; set; }

        // Assessment configuration (Exam module, 2026-07-28) -- HasTheory/HasPractical make
        // explicit what used to be inferred from whether TheoryMarks/PracticalMarks had a value;
        // TheoryPassMarks/PracticalPassMarks are the missing per-component pass thresholds
        // (TheoryMarks/PracticalMarks above were always full-marks only). A student must clear
        // BOTH the overall PassMarks AND each enabled component's own pass mark -- enforced at
        // mark-entry/result time, not here (this entity only stores the configured scheme).
        // Default HasTheory = true / HasPractical = false matches every subject configured before
        // this round (pure-theory was the overwhelmingly common case).
        public bool HasTheory { get; set; } = true;
        public bool HasPractical { get; set; }
        public int? TheoryPassMarks { get; set; }
        public int? PracticalPassMarks { get; set; }

        public virtual AcademicClass AcademicClass { get; set; }
        public virtual ClassSection ClassSection { get; set; }
    }
}
