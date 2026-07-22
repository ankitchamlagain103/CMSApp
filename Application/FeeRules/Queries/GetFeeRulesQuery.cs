using Domain.Enums;

namespace Application.FeeRules.Queries
{
    public class GetFeeRulesQuery
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public FeeRuleType? RuleType { get; set; }
        public Guid? AcademicClassId { get; set; }
        public bool? IsActive { get; set; }
    }
}
