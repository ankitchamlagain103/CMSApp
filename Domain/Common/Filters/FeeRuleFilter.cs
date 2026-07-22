using Domain.Enums;

namespace Domain.Common.Filters
{
    public class FeeRuleFilter
    {
        public FeeRuleType? RuleType { get; set; }
        public Guid? AcademicClassId { get; set; }
        public bool? IsActive { get; set; }
    }
}
