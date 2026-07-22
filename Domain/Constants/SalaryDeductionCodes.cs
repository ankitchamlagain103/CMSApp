namespace Domain.Constants
{
    // The well-known DeductionType (Config catalog 1014) codes the payroll code itself needs to
    // name: the employee-share SSF deduction and the optional CIT (Citizen Investment Trust)
    // savings deduction the salary calculator emits in its suggested structure. Same convention
    // as SalaryComponentCodes; LOAN/ADVANCE already have their own Domain/Constants/LoanTypeCodes.
    public static class SalaryDeductionCodes
    {
        public const string SsfDeduction = "SSF_DEDUCTION";
        public const string CitDeduction = "CIT_DEDUCTION";
    }
}
