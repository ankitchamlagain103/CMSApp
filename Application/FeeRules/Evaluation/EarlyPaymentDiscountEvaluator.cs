using Domain.Entities;
using Domain.Enums;

namespace Application.FeeRules.Evaluation
{
    // "Pay at least N days before the due date -> discount." Evaluated per fully-settled
    // invoice: qualifies when PaymentDate <= DueDate - DaysBeforeDueDate (with 0 meaning
    // "on or before the due date"). Percentage discounts the invoice's (category-scoped)
    // recurring subtotal; FixedAmount is capped at the invoice's remaining balance.
    public class EarlyPaymentDiscountEvaluator : IFeeRuleEvaluator
    {
        public List<FeeRuleDiscount> Evaluate(FeeRule rule, FeeRuleContext context)
        {
            var discounts = new List<FeeRuleDiscount>();

            var daysBefore = rule.DaysBeforeDueDate ?? 0;

            foreach (var invoice in context.FullySettledInvoices)
            {
                var latestQualifyingDate = invoice.DueDate.Date.AddDays(-daysBefore);
                if (context.PaymentDate.Date > latestQualifyingDate)
                {
                    continue;
                }

                decimal discountAmount;
                if (rule.ValueType == AwardValueType.Percentage)
                {
                    var baseAmount = FeeRuleEngine.ResolveBaseAmount(rule, invoice);
                    discountAmount = Math.Round(baseAmount * (rule.Value / 100m), 2);
                }
                else
                {
                    discountAmount = Math.Min(rule.Value, invoice.RemainingBalance);
                }

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
                    Description = rule.Name + " (paid before due date)"
                };
                discounts.Add(discount);
            }

            return discounts;
        }
    }
}
