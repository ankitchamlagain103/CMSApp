namespace Application.Employees.Dtos
{
    // One card in the Payslip tab's month grid. Only months whose PeriodStartDate has already
    // started are returned (see IEmployeeService.GetPayslipsAsync) -- a future fiscal month has no
    // payslip to show yet. Since the payroll-run redesign (2026-07-16), a month with a persisted
    // SalarySlip serves that slip's real figures (IsProjection = false); months without one keep
    // the read-time projection (PayDays = MonthDays, Upl 0 -- no attendance module) with
    // IsProjection = true.
    public class PayslipSummaryDto
    {
        public int MonthIndex { get; set; }
        public string MonthLabel { get; set; }
        public DateTime PeriodStartDate { get; set; }
        public DateTime PeriodEndDate { get; set; }
        public int MonthDays { get; set; }
        public int PayDays { get; set; }
        public int Upl { get; set; }
        public bool IsProjection { get; set; }
    }
}
