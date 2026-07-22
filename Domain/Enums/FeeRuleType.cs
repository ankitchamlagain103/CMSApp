namespace Domain.Enums
{
    // Which discount behavior a FeeRule encodes. Each member has a matching evaluator in
    // Application/FeeRules/Evaluation -- adding a member here means adding an evaluator there
    // and a switch arm in FeeRuleEngine (extension slots: LateFee, SiblingAutoDiscount, ...).
    public enum FeeRuleType
    {
        AdvanceMonthsDiscount = 1,
        EarlyPaymentDiscount = 2
    }
}
