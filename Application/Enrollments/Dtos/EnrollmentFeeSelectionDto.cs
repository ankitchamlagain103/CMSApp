namespace Application.Enrollments.Dtos
{
    public class EnrollmentFeeSelectionDto
    {
        public Guid Id { get; set; }
        public Guid EnrollmentId { get; set; }
        public Guid FeeStructureItemId { get; set; }
    }
}
