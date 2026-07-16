using Domain.Enums;

namespace Application.Fees.Dtos
{
    public class FeeStructureItemDto
    {
        public Guid Id { get; set; }
        public Guid FeeStructureId { get; set; }
        public string FeeCategoryCode { get; set; }
        public decimal Amount { get; set; }
        public FeeFrequencyType FrequencyType { get; set; }
        public bool IsOptional { get; set; }
        public bool IsRefundable { get; set; }
    }
}
