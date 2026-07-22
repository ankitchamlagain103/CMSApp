namespace Domain.Enums
{
    // Direction/kind of a SalarySlipLine. Amount is always stored positive; LineType decides
    // whether it adds to gross earnings (Earning) or to total deductions (Deduction/Tax/LoanEmi).
    // Provenance is the orthogonal SalaryLineSource -- e.g. a bonus adjustment becomes an
    // Earning line with Source = MonthlyAdjustment, an unpaid-leave adjustment a Deduction line.
    public enum SalaryLineType
    {
        Earning = 1,
        Deduction = 2,
        Tax = 3,
        LoanEmi = 4
    }
}
