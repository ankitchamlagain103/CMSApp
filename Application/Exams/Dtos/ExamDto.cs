namespace Application.Exams.Dtos
{
    // SubjectCode/InvigilatorName are flattened in from navigations (same convention as
    // EnrollmentDto's flattened GradeCode/SectionCode). No ClassSectionId/SectionCode -- an Exam
    // always covers the whole grade now (see the Exam entity's doc comment); GradeCode is
    // included instead, for display, sourced from the linked ClassSubject's AcademicClass.
    // FullMarks/PassMarks/etc. are READ-ONLY pass-through from the linked ClassSubject -- never
    // settable here, never stored on Exam itself. Configure them once via
    // POST/PUT /api/academicclasses/{id}/subjects[/{classSubjectId}] (grade-wise, optionally
    // section-wise), and every Exam for that subject reads the same values.
    public class ExamDto
    {
        public Guid Id { get; set; }
        public Guid ExamTermId { get; set; }
        public Guid ClassSubjectId { get; set; }
        public string SubjectCode { get; set; }
        public string GradeCode { get; set; }
        public DateTime ExamDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public Guid? InvigilatorEmployeeId { get; set; }
        public string InvigilatorName { get; set; }
        public string Remarks { get; set; }
        public bool MarksLocked { get; set; }
        public int? FullMarks { get; set; }
        public int? PassMarks { get; set; }
        public bool HasTheory { get; set; }
        public bool HasPractical { get; set; }
        public int? TheoryMarks { get; set; }
        public int? PracticalMarks { get; set; }
        public int? TheoryPassMarks { get; set; }
        public int? PracticalPassMarks { get; set; }
    }
}
