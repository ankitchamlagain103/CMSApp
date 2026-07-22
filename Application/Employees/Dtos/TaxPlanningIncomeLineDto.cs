using Domain.Enums;

namespace Application.Employees.Dtos
{
    // One row of the Investment & Tax Planning tab's "Income" table -- every earning component
    // on the current salary revision, taxable or not (IsTaxable decides whether it counts toward
    // TaxPlanningDto.TotalAnnualIncome, not whether it's listed here).
    public class TaxPlanningIncomeLineDto
    {
        public string Code { get; set; }
        public string Label { get; set; }
        public AwardValueType ValueType { get; set; }
        public decimal AnnualAmount { get; set; }
        public bool IsTaxable { get; set; }
    }
}
