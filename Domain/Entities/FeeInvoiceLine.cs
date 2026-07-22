using Domain.Enums;

namespace Domain.Entities
{
    // A single signed line on a FeeInvoice: positive = charge, negative = discount/credit.
    // Hard-deleted (pure line item, like FeeStructureItem), mutable only while the invoice is
    // Draft -- except RuleDiscount lines, the one machine-generated post-finalization append
    // (payment-time fee rules).
    //
    // The lineage ids (FeeStructureItemId/StudentDiscountId/StudentScholarshipId/FeeRuleId/
    // FeeAdjustmentId) are deliberately plain scalar columns with NO database FK: an invoice
    // line is a snapshot (category/description/amount are copied at generation), and the
    // configuration row that produced it must stay freely editable/deletable without being
    // blocked by -- or cascading into -- historical financial records.
    public class FeeInvoiceLine : AuditableEntity
    {
        public Guid Id { get; set; }
        public Guid FeeInvoiceId { get; set; }
        public FeeLineSource Source { get; set; }
        public Guid? FeeStructureItemId { get; set; }
        public Guid? StudentDiscountId { get; set; }
        public Guid? StudentScholarshipId { get; set; }
        public Guid? FeeRuleId { get; set; }
        public Guid? FeeAdjustmentId { get; set; }
        public string FeeCategoryCode { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }

        public virtual FeeInvoice FeeInvoice { get; set; }
    }
}
