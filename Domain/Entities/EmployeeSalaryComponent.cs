using Domain.Enums;

namespace Domain.Entities
{
    // One income line item on a salary revision (Basic Salary, SSF Contribution, allowances,
    // festival bonus, leave encashment, ...). ComponentCode is a Config code
    // (ConfigTypeCodes.SalaryComponentType), validated in the service layer, not a database FK.
    // Percentage-valued components (ValueType.Percentage) resolve their rate against the sibling
    // "BASIC" component (Domain/Constants/SalaryComponentCodes.Basic) in the same EmployeeSalary
    // revision. IsTaxable feeds gross-annual-income assembly; IsRetirementContribution feeds the
    // retirement-fund exemption calculation (TaxCalculator). Hard-deleted (pure line item, like
    // ClassSubject).
    public class EmployeeSalaryComponent : AuditableEntity
    {
        public Guid Id { get; set; }
        public Guid EmployeeSalaryId { get; set; }
        public string ComponentCode { get; set; }
        public AwardValueType ValueType { get; set; }
        public decimal Value { get; set; }
        public PayFrequencyType FrequencyType { get; set; }
        public bool IsTaxable { get; set; }
        public bool IsRetirementContribution { get; set; }
        public virtual EmployeeSalary EmployeeSalary { get; set; }
    }
}
