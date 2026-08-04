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
    // needed for the simple "assign subject, date, time" scheduling flow this module actually
    // needs). No invigilator concept either (removed same day, per instruction) -- Exam is just
    // subject + date/time.
    public class Exam : AuditableEntity
    {
        public Guid Id { get; set; }
        public Guid ExamTermId { get; set; }
        public Guid ClassSubjectId { get; set; }
        public DateTime ExamDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }

        // Class period (2026-07-30, moved off the Config catalog onto a real FK 2026-08-03 -- see
        // TimePeriod's doc comment for why: different classes can run different period
        // structures, which a flat Config option list can't express). Optional: when set,
        // StartTime/EndTime are RESOLVED from the period's own StartTime/EndTime at create/update
        // time (see ExamService.ResolveExamTimesAsync) rather than entered raw -- StartTime/EndTime
        // always end up populated either way, this is just an alternate, TimePeriod-driven input
        // path. A Break-typed period is rejected (an exam can't sit during lunch), and the picked
        // period must be mapped (via ClassTimePeriod) to the exam's own class.
        public Guid? TimePeriodId { get; set; }
        public string Remarks { get; set; }

        // Design doc step 6, "Lock Marks: Once entry window closes, administrator locks entries
        // to prevent edits." Scoped per exam (one subject's one sitting).
        public bool MarksLocked { get; set; }

        // Plain scalar lineage column, no FK -- see the doc comment this carried before the
        // merge (same convention as FeeInvoiceLine's lineage ids).
        public Guid? CalendarEventId { get; set; }

        public virtual ExamTerm ExamTerm { get; set; }
        public virtual ClassSubject ClassSubject { get; set; }
        public virtual TimePeriod TimePeriod { get; set; }
        public virtual ICollection<StudentExamMark> Marks { get; set; } = new List<StudentExamMark>();
    }
}
