namespace Application.Employees.Dtos
{
    // One card in the Payslip tab's month grid. Only months whose PeriodStartDate has already
    // started are returned (see IEmployeeService.GetPayslipsAsync) -- a future fiscal month has no
    // payslip to show yet. PayDays/Upl are simplified (PayDays = MonthDays, Upl always 0) since
    // this codebase has no attendance/leave module to source real figures from.
    public class PayslipSummaryDto
    {
        public int MonthIndex { get; set; }
        public string MonthLabel { get; set; }
        public DateTime PeriodStartDate { get; set; }
        public DateTime PeriodEndDate { get; set; }
        public int MonthDays { get; set; }
        public int PayDays { get; set; }
        public int Upl { get; set; }
    }
}
