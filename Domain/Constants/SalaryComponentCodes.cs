namespace Domain.Constants
{
    // The well-known SalaryComponentType codes the payroll code itself needs to name: Basic is
    // what Percentage-valued components/deductions resolve their rate against (e.g. "SSF
    // Deduction, 11% of Basic Salary"); SsfContribution/OtherAllowance are the codes the salary
    // calculator emits in its suggested structure. Documented convention rather than magic
    // strings scattered through the codebase -- the full admin-extensible catalog lives in
    // Config type 1013.
    public static class SalaryComponentCodes
    {
        public const string Basic = "BASIC";
        public const string SsfContribution = "SSF_CONTRIBUTION";
        public const string OtherAllowance = "OTHER_ALLOWANCE";
        public const string FestivalBonus = "FESTIVAL_BONUS";
    }
}
