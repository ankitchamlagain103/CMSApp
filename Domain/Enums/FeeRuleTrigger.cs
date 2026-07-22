namespace Domain.Enums
{
    // When a FeeRule is evaluated. Both shipped rule types are OnPayment (they depend on how a
    // payment settles invoices); OnGeneration exists so future rules (e.g. a late fee charged on
    // months with overdue prior invoices) slot in without a schema change.
    public enum FeeRuleTrigger
    {
        OnPayment = 1,
        OnGeneration = 2
    }
}
