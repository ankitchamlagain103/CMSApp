namespace Application.AcademicClasses.Commands
{
    // ClassSectionId scopes an OPTIONAL subject to one section of the class (null = offered to
    // every section). Mandatory subjects are always class-wide, so IsMandatory = true with a
    // ClassSectionId is rejected. Grading fields (2026-07-15) are all optional -- a subject can be
    // assigned before its marks scheme is finalized, and filled in later via the update endpoint.
    public class AssignClassSubjectCommand
    {
        public string SubjectCode { get; set; }
        public bool IsMandatory { get; set; } = true;
        public int DisplayOrder { get; set; }
        public Guid? ClassSectionId { get; set; }
        public decimal? CreditHours { get; set; }
        public int? FullMarks { get; set; }
        public int? PassMarks { get; set; }
        public int? TheoryMarks { get; set; }
        public int? PracticalMarks { get; set; }

        // Assessment configuration (2026-07-28) -- see the doc comment on Domain/Entities/ClassSubject.
        public bool HasTheory { get; set; } = true;
        public bool HasPractical { get; set; }
        public int? TheoryPassMarks { get; set; }
        public int? PracticalPassMarks { get; set; }
    }
}
