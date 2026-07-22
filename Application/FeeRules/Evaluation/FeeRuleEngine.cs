using Domain.Entities;
using Domain.Enums;

namespace Application.FeeRules.Evaluation
{
    // Applies the active, scope-matching rules to a payment context in Priority order. Pure/
    // static like TaxCalculator: rules are loaded by the caller (IFeeRuleRepository.
    // GetActiveRulesAsync), so the engine itself does no I/O and is unit-testable standalone.
    //
    // Combinability: the first matching rule always applies; after that, only IsCombinable
    // rules stack on top -- a non-combinable rule that isn't first is skipped, and once a
    // non-combinable rule has applied first, nothing else stacks (it "wins" outright).
    public static class FeeRuleEngine
    {
        public static List<FeeRuleDiscount> Evaluate(IReadOnlyList<FeeRule> orderedRules, FeeRuleContext context)
        {
            var appliedDiscounts = new List<FeeRuleDiscount>();
            var firstAppliedRuleWasExclusive = false;

            foreach (var rule in orderedRules)
            {
                if (appliedDiscounts.Count > 0 && (firstAppliedRuleWasExclusive || !rule.IsCombinable))
                {
                    continue;
                }

                if (!MatchesScope(rule, context))
                {
                    continue;
                }

                var evaluator = ResolveEvaluator(rule.RuleType);
                if (evaluator == null)
                {
                    continue;
                }

                var ruleDiscounts = evaluator.Evaluate(rule, context);
                if (ruleDiscounts.Count == 0)
                {
                    continue;
                }

                if (appliedDiscounts.Count == 0 && !rule.IsCombinable)
                {
                    firstAppliedRuleWasExclusive = true;
                }

                appliedDiscounts.AddRange(ruleDiscounts);
            }

            return appliedDiscounts;
        }

        // Percentage discounts resolve against the invoice's recurring subtotal, narrowed to
        // one category when the rule is category-scoped. Internal to the Evaluation namespace's
        // evaluators.
        internal static decimal ResolveBaseAmount(FeeRule rule, FeeRuleInvoiceContext invoice)
        {
            if (string.IsNullOrWhiteSpace(rule.FeeCategoryCode))
            {
                return invoice.RecurringSubtotal;
            }

            return invoice.RecurringSubtotalByCategory.TryGetValue(rule.FeeCategoryCode, out var categoryAmount)
                ? categoryAmount
                : 0m;
        }

        private static bool MatchesScope(FeeRule rule, FeeRuleContext context)
        {
            if (rule.AcademicClassId.HasValue && rule.AcademicClassId.Value != context.AcademicClassId)
            {
                return false;
            }

            return true;
        }

        private static IFeeRuleEvaluator ResolveEvaluator(FeeRuleType ruleType)
        {
            switch (ruleType)
            {
                case FeeRuleType.AdvanceMonthsDiscount:
                    return new AdvanceMonthsDiscountEvaluator();
                case FeeRuleType.EarlyPaymentDiscount:
                    return new EarlyPaymentDiscountEvaluator();
                default:
                    return null;
            }
        }
    }
}
