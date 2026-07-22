using Application.Payroll.Dtos;

namespace Application.Employees.Dtos
{
    // The structured (non-HTML) payslip detail behind the Payslip tab's modal -- a separate path
    // from the existing GetPayslipPreviewAsync (which renders the admin-configured HTML template
    // for the latest revision only). IncomeLines/DeductionLines reuse MonthlyLineItemDto, the same
    // {Code, Amount} shape MonthlyBreakdownCalculator already produces, so this detail can never
    // disagree with the Tax Details monthly table for the same month. DeductionLines additionally
    // includes that month's flat TDS share and any active EmployeeLoan's EMI (see
    // IEmployeeService.GetPayslipDetailAsync).
    public class PayslipDetailDto
    {
        public string EmployeeName { get; set; }
        public string EmployeeCode { get; set; }
        public string JobPositionCode { get; set; }
        public string PayMonthLabel { get; set; }
        public int MonthDays { get; set; }
        public int PayDays { get; set; }
        public int Upl { get; set; }
        public List<MonthlyLineItemDto> IncomeLines { get; set; } = new List<MonthlyLineItemDto>();
        public decimal GrossIncome { get; set; }
        public List<MonthlyLineItemDto> DeductionLines { get; set; } = new List<MonthlyLineItemDto>();
        public decimal TotalDeduction { get; set; }
        public decimal NetPaid { get; set; }

        // False when this detail comes from a persisted SalarySlip (a generated payroll run);
        // true when it's the read-time projection for a month with no run yet (P8).
        public bool IsProjection { get; set; }
    }
}
