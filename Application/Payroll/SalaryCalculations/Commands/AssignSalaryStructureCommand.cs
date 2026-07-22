namespace Application.Payroll.SalaryCalculations.Commands
{
    // "Assign to an employee" from the calculator: the same calculation inputs plus who gets the
    // structure and from when. The service re-runs the calculation server-side and persists the
    // suggested lines as a real salary revision through the normal
    // POST /api/employees/{id}/salaries path (same validations, same audit trail).
    public class AssignSalaryStructureCommand : CalculateSalaryStructureCommand
    {
        public Guid EmployeeId { get; set; }
        public DateTime EffectiveFromDate { get; set; }
    }
}
