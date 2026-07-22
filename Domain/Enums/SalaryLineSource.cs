namespace Domain.Enums
{
    // Where a SalarySlipLine came from: the compensation plan snapshot (SalaryStructure), the
    // tax calculation (TaxCalculator), a due loan EMI (LoanSchedule), a pre-run monthly
    // SalaryAdjustment (MonthlyAdjustment), or a manual edit on the Draft slip (Manual).
    public enum SalaryLineSource
    {
        SalaryStructure = 1,
        TaxCalculator = 2,
        LoanSchedule = 3,
        MonthlyAdjustment = 4,
        Manual = 5
    }
}
