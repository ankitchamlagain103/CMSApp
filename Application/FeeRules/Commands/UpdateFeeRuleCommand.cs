using Domain.Enums;

namespace Application.FeeRules.Commands
{
    // Code and RuleType are immutable on update (identity-bearing, same convention as
    // AcademicYear.Code) -- a differently-shaped rule is a new rule.
    public class UpdateFeeRuleCommand
    {
        public string Name { get; set; }
        public AwardValueType ValueType { get; set; }
        public decimal Value { get; set; }
        public int? MinMonthsTogether { get; set; }
        public int? DaysBeforeDueDate { get; set; }
        public Guid? AcademicClassId { get; set; }
        public string FeeCategoryCode { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public int Priority { get; set; }
        public bool IsCombinable { get; set; }
        public bool IsActive { get; set; }
    }
}
