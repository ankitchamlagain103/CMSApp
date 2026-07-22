using Domain.Entities;

namespace Application.FeeRules.Evaluation
{
    // One evaluator per FeeRuleType. Evaluators are pure (no I/O): they read the already-
    // scope-filtered rule plus the context and return zero or more proposed discounts.
    public interface IFeeRuleEvaluator
    {
        List<FeeRuleDiscount> Evaluate(FeeRule rule, FeeRuleContext context);
    }
}
