using Domain.Enums;

namespace Application.FeeRules.Dtos
{
    public class FeeRuleDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public FeeRuleType RuleType { get; set; }
        public FeeRuleTrigger TriggerStage { get; set; }
        public AwardValueType ValueType { get; set; }
        public decimal Value { get; set; }
        public int? MinMonthsTogether { get; set; }
        public int? DaysBeforeDueDate { get; set; }
        public Guid? AcademicClassId { get; set; }
        public string AcademicClassGradeCode { get; set; }
        public string FeeCategoryCode { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public int Priority { get; set; }
        public bool IsCombinable { get; set; }
        public bool IsActive { get; set; }
    }
}
