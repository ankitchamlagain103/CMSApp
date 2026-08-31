namespace Application.Exams.Dtos
{
    // No skip list -- the save is all-or-nothing (Application/Exams/ExamService.SaveExamRoutineAsync
    // validates the complete timetable before touching anything, and fails the whole request on any
    // violation instead of partially applying it). Items is the resulting full set of exams for this
    // (ExamTermId, AcademicClassId) pair after the save, so a caller never needs a follow-up GET.
    public class SaveExamRoutineResultDto
    {
        public Guid ExamTermId { get; set; }
        public Guid AcademicClassId { get; set; }
        public int CreatedCount { get; set; }
        public int UpdatedCount { get; set; }
        public int DeletedCount { get; set; }
        public List<ExamDto> Items { get; set; } = new List<ExamDto>();
    }
}
