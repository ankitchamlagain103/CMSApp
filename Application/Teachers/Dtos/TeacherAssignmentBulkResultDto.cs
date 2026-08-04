namespace Application.Teachers.Dtos
{
    public class TeacherAssignmentBulkResultDto
    {
        public List<TeacherAssignmentDto> Created { get; set; } = new List<TeacherAssignmentDto>();
        public List<TeacherAssignmentSkipDto> Skipped { get; set; } = new List<TeacherAssignmentSkipDto>();
    }
}
