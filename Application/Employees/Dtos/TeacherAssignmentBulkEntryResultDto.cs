namespace Application.Employees.Dtos
{
    public class TeacherAssignmentBulkEntryResultDto
    {
        public List<TeacherAssignmentDto> Created { get; set; } = new List<TeacherAssignmentDto>();
        public List<TeacherAssignmentEntrySkipDto> Skipped { get; set; } = new List<TeacherAssignmentEntrySkipDto>();
    }
}
