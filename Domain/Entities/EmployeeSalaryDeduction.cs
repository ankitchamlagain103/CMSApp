using Domain.Enums;

namespace Domain.Entities
{
    // One deduction/loan/advance line item on a salary revision -- reduces take-home pay, so
    // (unlike EmployeeSalaryComponent) there is no IsTaxable flag. DeductionCode is a Config code
    // (ConfigTypeCodes.DeductionType), validated in the service layer, not a database FK.
    // IsRetirementContribution flags e.g. "SSF Deduction" so it feeds the retirement-fund
    // exemption calculation alongside any retirement-flagged income components. Percentage-valued
    // deductions resolve against the sibling "BASIC" component, same as EmployeeSalaryComponent.
    // Hard-deleted (pure line item).
    public class EmployeeSalaryDeduction : AuditableEntity
    {
        public Guid Id { get; set; }
        public Guid EmployeeSalaryId { get; set; }
        public string DeductionCode { get; set; }
        public AwardValueType ValueType { get; set; }
        public decimal Value { get; set; }
        public PayFrequencyType FrequencyType { get; set; }
        public bool IsRetirementContribution { get; set; }
        public virtual EmployeeSalary EmployeeSalary { get; set; }
    }
}
