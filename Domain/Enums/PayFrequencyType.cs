namespace Domain.Enums
{
    // Payroll-named twin of FeeFrequencyType (same 3 values) -- kept separate so Payroll code
    // doesn't depend on a Fees-named enum.
    public enum PayFrequencyType
    {
        Monthly = 1,
        Annual = 2,
        OneTime = 3
    }
}
