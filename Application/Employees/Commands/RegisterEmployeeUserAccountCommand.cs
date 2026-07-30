namespace Application.Employees.Commands
{
    // Retrofit path for an Employee created before opting into a portal account (or one whose
    // admin skipped the checkbox at creation time) -- POST /api/employees/{id}/register-account.
    // Reuses the employee's existing Email; RoleIds is admin-picked, same as CreateEmployeeCommand.
    public class RegisterEmployeeUserAccountCommand
    {
        public List<Guid> RoleIds { get; set; } = new List<Guid>();
    }
}
