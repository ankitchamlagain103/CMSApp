using Domain.Enums;

namespace Application.FeeRules.Commands
{
    // TriggerStage is deliberately NOT a field: it's derived from RuleType by the service
    // (both shipped rule types evaluate at payment time), so a caller can't create a rule
    // whose trigger doesn't match its type's evaluator.
    public class CreateFeeRuleCommand
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public FeeRuleType RuleType { get; set; }
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
        public bool IsActive { get; set; } = true;
    }
}
