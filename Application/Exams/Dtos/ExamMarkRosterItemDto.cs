namespace Application.Exams.Dtos
{
    // One row per enrolled student in an exam's section -- the "search and select a student"
    // marks-entry worklist. Mark is null when the student hasn't been marked yet (the UI shows an
    // empty entry row); non-null when editing an already-recorded mark (prefill from it, then
    // PUT /api/studentexammarks/{mark.id} instead of POST).
    public class ExamMarkRosterItemDto
    {
        public Guid EnrollmentId { get; set; }
        public Guid StudentId { get; set; }
        public string AdmissionNo { get; set; }
        public string StudentName { get; set; }
        public string RollNumber { get; set; }
        public StudentExamMarkDto Mark { get; set; }
    }
}
