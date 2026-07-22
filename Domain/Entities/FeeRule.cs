using Domain.Enums;

namespace Domain.Entities
{
    // A configurable fee discount rule ("pay X months together -> Y% off", "pay N days before
    // the due date -> flat Z off"), evaluated by Application/FeeRules/Evaluation/FeeRuleEngine
    // at the rule's TriggerStage. Soft-deleted (financial-audit configuration, like
    // StudentDiscount). The nullable parameter columns (MinMonthsTogether/DaysBeforeDueDate)
    // are per-RuleType inputs -- a genuinely new rule shape gets a new nullable column, which
    // is acceptable churn for a low-volume configuration table.
    public class FeeRule : SoftDeleteAuditableEntity
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public FeeRuleType RuleType { get; set; }
        public FeeRuleTrigger TriggerStage { get; set; }
        public AwardValueType ValueType { get; set; }
        public decimal Value { get; set; }

        // AdvanceMonthsDiscount: minimum number of invoice-months a single payment must fully
        // settle for the discount to apply.
        public int? MinMonthsTogether { get; set; }

        // EarlyPaymentDiscount: pay at least this many days before the invoice's DueDate
        // (0 = any time on/before the due date).
        public int? DaysBeforeDueDate { get; set; }

        // Scope filters: null = applies to all classes / the whole recurring subtotal.
        public Guid? AcademicClassId { get; set; }
        public string FeeCategoryCode { get; set; }

        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public int Priority { get; set; }
        public bool IsCombinable { get; set; }
        public bool IsActive { get; set; }

        public virtual AcademicClass AcademicClass { get; set; }
    }
}
