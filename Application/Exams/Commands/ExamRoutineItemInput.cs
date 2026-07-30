namespace Application.Exams.Commands
{
    // One subject's sitting within a CreateExamRoutineCommand -- ClassSubjectId must belong to
    // the command's own AcademicClassId (checked in the service, not here).
    public class ExamRoutineItemInput
    {
        public Guid ClassSubjectId { get; set; }
        public DateTime ExamDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public Guid? InvigilatorEmployeeId { get; set; }
        public string Remarks { get; set; }
    }
}
