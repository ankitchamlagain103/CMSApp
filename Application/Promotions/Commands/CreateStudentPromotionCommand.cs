using Domain.Enums;

namespace Application.Promotions.Commands
{
    // Manual, single-student promotion/retention/transfer -- covers all three PromotionType
    // values with one shape, since each is mechanically the same operation (close the old
    // enrollment, open a new one in ToClassSectionId, log the transition).
    public class CreateStudentPromotionCommand
    {
        public Guid FromEnrollmentId { get; set; }
        public Guid ToClassSectionId { get; set; }
        public string RollNumber { get; set; }
        public DateTime PromotionDate { get; set; }
        public PromotionType PromotionType { get; set; }
        public string Remarks { get; set; }
    }
}
