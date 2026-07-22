namespace Domain.Constants
{
    // The two well-known option codes of the SsfRate Config catalog (ConfigTypeCodes.SsfRate,
    // 1018). Nepal's Social Security Fund is a 31%-of-Basic-Salary scheme split into an employee
    // share (deducted from pay, statutorily 11%) and an employer share (paid on top of salary,
    // statutorily 20%); the actual percentages live in each option's AdditionalValue1 so an admin
    // can adjust them when the law changes -- these constants only name the rows.
    public static class SsfShareCodes
    {
        public const string EmployeeShare = "EMPLOYEE_SHARE";
        public const string EmployerShare = "EMPLOYER_SHARE";
    }
}
