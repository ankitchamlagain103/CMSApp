namespace Domain.Entities
{
    // One exam sitting per subject per term -- unique on (ExamTermId, ClassSubjectId). Per the
    // revised design (Docs/Exam_Module_Design_Revised.md), an Exam is deliberately NOT scoped to
    // a ClassSection: it always covers the whole grade (ClassSubject.AcademicClassId), and which
    // students are actually eligible is resolved from the subject's mandatory/elective status at
    // read/generation time (see Application/Exams/EligibleEnrollmentResolver), not by pinning a
    // section here. This replaces the earlier design's Name/WeightagePercent/IsFinalExam
    // (multiple named sittings per subject per term, weighted composite grading) -- the revised
    // guide drops that entirely in favor of exactly one graded sitting per subject per term.
    // Hard-deleted -- a pure child of ExamTerm, same "line item under an identity-bearing parent"
    // convention as ClassSubject under AcademicClass.
    //
    // Deliberately has NO MaximumMarks/PassMarks of its own -- those are configured exactly once,
    // grade-wise (and optionally section-wise, via ClassSubject.ClassSectionId), on ClassSubject
    // itself (see ClassSubject.FullMarks/PassMarks/TheoryMarks/PracticalMarks).
    //
    // No Room/seat-arrangement concept (removed 2026-07-30, per instruction -- the dedicated
    // ExamRoom/ExamHallArrangement/ExamSeatAllocation subsystem was dropped entirely; a room isn't
    // needed for the simple "assign subject, date, time, invigilator" scheduling flow this
    // module actually needs). InvigilatorEmployeeId stays optional -- it's a real Employee, not a
    // dedicated table, so nothing to remove there.
    public class Exam : AuditableEntity
    {
        public Guid Id { get; set; }
        public Guid ExamTermId { get; set; }
        public Guid ClassSubjectId { get; set; }
        public DateTime ExamDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public Guid? InvigilatorEmployeeId { get; set; }
        public string Remarks { get; set; }

        // Design doc step 6, "Lock Marks: Once entry window closes, administrator locks entries
        // to prevent edits." Scoped per exam (one subject's one sitting).
        public bool MarksLocked { get; set; }

        // Plain scalar lineage column, no FK -- see the doc comment this carried before the
        // merge (same convention as FeeInvoiceLine's lineage ids).
        public Guid? CalendarEventId { get; set; }

        public virtual ExamTerm ExamTerm { get; set; }
        public virtual ClassSubject ClassSubject { get; set; }
        public virtual Employee InvigilatorEmployee { get; set; }
        public virtual ICollection<StudentExamMark> Marks { get; set; } = new List<StudentExamMark>();
    }
}
