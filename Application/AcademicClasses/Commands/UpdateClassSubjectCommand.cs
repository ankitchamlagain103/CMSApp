namespace Application.AcademicClasses.Commands
{
    // Only the grading metadata + DisplayOrder are updatable -- SubjectCode/IsMandatory/
    // ClassSectionId are identity-like (same convention as UpdateAcademicClassCommand only
    // allowing Status: changing them would mean re-assigning the subject, not editing it).
    public class UpdateClassSubjectCommand
    {
        public int DisplayOrder { get; set; }
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
