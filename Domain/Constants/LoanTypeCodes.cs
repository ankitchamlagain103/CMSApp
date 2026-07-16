namespace Domain.Constants
{
    // The two DeductionType (ConfigTypeCodes.DeductionType) catalog codes that EmployeeLoan.
    // LoanTypeCode is restricted to -- the catalog also has SSF_DEDUCTION/OTHER, which are not
    // loan-shaped, so the service checks against these two specifically rather than "any
    // DeductionType code".
    public static class LoanTypeCodes
    {
        public const string Loan = "LOAN";
        public const string Advance = "ADVANCE";
    }
}
