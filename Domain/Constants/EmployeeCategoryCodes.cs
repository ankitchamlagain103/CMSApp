namespace Domain.Constants
{
    // The one well-known EmployeeCategory code EmployeeRoleHelper.IsTeachingStaff checks against
    // (alongside JobPositionCodes.Teacher/Principal/VicePrincipal) to derive whether an Employee
    // counts as "teaching staff" for the dashboard widget / global search.
    public static class EmployeeCategoryCodes
    {
        public const string Academic = "ACADEMIC";
    }
}
