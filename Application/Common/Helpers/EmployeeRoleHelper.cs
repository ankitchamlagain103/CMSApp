using Domain.Constants;

namespace Application.Common.Helpers
{
    // "Is this employee teaching staff" -- a read-only derived predicate (2026-08-06, replacing
    // the removed standalone Teacher entity/profile). Shared by the dashboard teacher widget,
    // global search's IsTeacher flag, and EmployeeMapper's IsTeachingStaff DTO field, so the rule
    // lives in exactly one place. Not a gate on any action -- TeachingLicenseNo/ExperienceYears/
    // Specialization may be set on any Employee regardless of this predicate.
    public static class EmployeeRoleHelper
    {
        public static bool IsTeachingStaff(string employeeCategoryCode, string jobPositionCode)
        {
            var isTeachingStaff = employeeCategoryCode == EmployeeCategoryCodes.Academic
                && (jobPositionCode == JobPositionCodes.Teacher
                    || jobPositionCode == JobPositionCodes.Principal
                    || jobPositionCode == JobPositionCodes.VicePrincipal);

            return isTeachingStaff;
        }
    }
}
