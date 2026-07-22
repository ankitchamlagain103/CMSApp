using Application.Payroll.Dtos;

namespace Application.Employees.Dtos
{
    // The single composite response behind the Investment & Tax Planning tab -- everything that
    // tab renders (income lines, the retirement fund a/b/c breakdown, insurance lines, assessment
    // type, and the annual tax calculation with its slab breakdown) in one call, computed
    // server-side from one TaxCalculator.CalculateFromSalary run so none of the figures can ever
    // disagree with each other or with GET .../salaries/tax-calculation (which this composes on
    // top of -- see IEmployeeService.GetTaxPlanningAsync).
    public class TaxPlanningDto
    {
        public Guid EmployeeId { get; set; }
        public Guid SalaryId { get; set; }
        public Guid FiscalYearId { get; set; }
        public string FiscalYearCode { get; set; }
        public string AssessmentType { get; set; }
        public List<TaxPlanningIncomeLineDto> IncomeLines { get; set; } = new List<TaxPlanningIncomeLineDto>();
        public decimal TotalAnnualIncome { get; set; }
        public RetirementFundBreakdownDto RetirementFund { get; set; }
        public List<TaxPlanningInsuranceLineDto> InsuranceLines { get; set; } = new List<TaxPlanningInsuranceLineDto>();
        public decimal InsuranceDeductionCapped { get; set; }
        public TaxCalculationResultDto TaxCalculation { get; set; }
        public decimal GrossMonthly { get; set; }
        public decimal NetMonthly { get; set; }
    }
}
