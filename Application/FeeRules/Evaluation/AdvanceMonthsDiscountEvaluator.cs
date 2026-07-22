using Domain.Entities;
using Domain.Enums;

namespace Application.FeeRules.Evaluation
{
    // "Pay X or more months together -> discount." Applies when the single payment fully
    // settles at least MinMonthsTogether invoices. A Percentage value discounts every settled
    // invoice's (category-scoped) recurring subtotal; a FixedAmount value is a one-off discount
    // attached to the newest settled invoice.
    public class AdvanceMonthsDiscountEvaluator : IFeeRuleEvaluator
    {
        public List<FeeRuleDiscount> Evaluate(FeeRule rule, FeeRuleContext context)
        {
            var discounts = new List<FeeRuleDiscount>();

            var minMonths = rule.MinMonthsTogether ?? 0;
            if (minMonths < 2 || context.FullySettledInvoices.Count < minMonths)
            {
                return discounts;
            }

            if (rule.ValueType == AwardValueType.Percentage)
            {
                foreach (var invoice in context.FullySettledInvoices)
                {
                    var baseAmount = FeeRuleEngine.ResolveBaseAmount(rule, invoice);
                    var discountAmount = Math.Round(baseAmount * (rule.Value / 100m), 2);
                    if (discountAmount <= 0m)
                    {
                        continue;
                    }

                    var discount = new FeeRuleDiscount
                    {
                        FeeRuleId = rule.Id,
                        RuleCode = rule.Code,
                        RuleName = rule.Name,
                        FeeInvoiceId = invoice.FeeInvoiceId,
                        Amount = discountAmount,
                        Description = rule.Name + " (" + context.FullySettledInvoices.Count + " months paid together)"
                    };
                    discounts.Add(discount);
                }

                return discounts;
            }

            var lastInvoice = context.FullySettledInvoices[context.FullySettledInvoices.Count - 1];
            var cappedAmount = Math.Min(rule.Value, lastInvoice.RemainingBalance);
            if (cappedAmount <= 0m)
            {
                return discounts;
            }

            var fixedDiscount = new FeeRuleDiscount
            {
                FeeRuleId = rule.Id,
                RuleCode = rule.Code,
                RuleName = rule.Name,
                FeeInvoiceId = lastInvoice.FeeInvoiceId,
                Amount = cappedAmount,
                Description = rule.Name + " (" + context.FullySettledInvoices.Count + " months paid together)"
            };
            discounts.Add(fixedDiscount);

            return discounts;
        }
    }
}
