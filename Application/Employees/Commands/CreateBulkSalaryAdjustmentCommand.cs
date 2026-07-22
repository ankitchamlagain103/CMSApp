namespace Application.Employees.Commands
{
    // One adjustment stamped onto many employees in a single call -- the "Dashain allowance /
    // festival bonus / leave encashment for everyone" case (mirrors the fee side's bulk
    // adjustment). Scope resolution: a non-empty EmployeeIds list wins (exactly those
    // employees); otherwise every payroll-eligible employee (Active/OnLeave with a compensation
    // plan), optionally narrowed by EmployeeCategoryCode. Everything else behaves exactly like
    // the single-employee CreateSalaryAdjustmentCommand it inherits from.
    public class CreateBulkSalaryAdjustmentCommand : CreateSalaryAdjustmentCommand
    {
        public List<Guid> EmployeeIds { get; set; } = new List<Guid>();
        public string EmployeeCategoryCode { get; set; }
    }
}
