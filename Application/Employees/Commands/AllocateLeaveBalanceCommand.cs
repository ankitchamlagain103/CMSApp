namespace Application.Employees.Commands
{
    // Upsert: allocates (or re-allocates) a leave balance for one employee/leave type/fiscal
    // year. FiscalYearId is optional -- blank resolves to whichever FiscalYear is IsCurrent, same
    // convention GetCurrentSalaryTaxCalculationAsync/GetTaxPlanningAsync already use.
    public class AllocateLeaveBalanceCommand
    {
        public Guid LeaveTypeId { get; set; }
        public Guid? FiscalYearId { get; set; }
        public decimal Allocated { get; set; }
    }
}
