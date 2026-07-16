namespace Application.Employees.Dtos
{
    public class TeacherProfileDto
    {
        public Guid EmployeeId { get; set; }
        public string TeachingLicenseNo { get; set; }
        public int? ExperienceYears { get; set; }
        public string Specialization { get; set; }
    }
}
