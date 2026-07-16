namespace Domain.Enums
{
    // Shared by StudentDiscount and StudentScholarship: whether Value is a percentage off the
    // class's MonthlyFeeAmount or a flat currency amount.
    public enum AwardValueType
    {
        Percentage = 1,
        FixedAmount = 2
    }
}
