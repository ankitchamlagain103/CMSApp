namespace Domain.Constants
{
    // The one well-known SalaryComponentType code that Percentage-valued components/deductions
    // resolve their rate against (e.g. "SSF Deduction, 31% of Basic Salary"). Documented
    // convention rather than a magic string scattered through the codebase.
    public static class SalaryComponentCodes
    {
        public const string Basic = "BASIC";
    }
}
