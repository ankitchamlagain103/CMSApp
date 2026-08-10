namespace Application.Employees.Dtos
{
    public class TeacherAssignmentSkipDto
    {
        public Guid ClassSectionId { get; set; }
        public string Reason { get; set; }
    }
}
