namespace Application.Promotions.Dtos
{
    public class BulkPromotionSkipDto
    {
        public Guid EnrollmentId { get; set; }
        public string StudentName { get; set; }
        public string Reason { get; set; }
    }
}
