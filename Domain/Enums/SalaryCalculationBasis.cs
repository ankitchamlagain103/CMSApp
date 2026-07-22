namespace Domain.Enums
{
    // Which figure the HR salary calculator treats as the fixed input when structuring a
    // compensation plan: the employee's take-home (NetPayment), the payable gross before
    // deductions (GrossPayment), or the employer's total cost including the employer-side SSF
    // contribution (Ctc).
    public enum SalaryCalculationBasis
    {
        NetPayment = 1,
        GrossPayment = 2,
        Ctc = 3
    }
}
