namespace Domain.Enums
{
    // How often a FeeStructure line item is billed. Admin-set per (class, category) row rather
    // than fixed by category, since schools differ on e.g. whether Computer Fee is monthly or
    // annual.
    public enum FeeFrequencyType
    {
        Monthly = 1,
        Annual = 2,
        OneTime = 3
    }
}
