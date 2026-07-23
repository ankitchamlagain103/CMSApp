using Domain.Enums;

namespace Application.Employees.Commands
{
    // Code-driven counterpart of SalaryComponentInput/SalaryDeductionInput (2026-07-23) -- the
    // caller supplies only a Code and EmployeeService.AddSalaryLineAsync resolves whether it's a
    // SalaryComponentType (1013) or DeductionType (1014) option and builds the matching entity.
    // IsTaxable is ignored when Code resolves to a deduction (EmployeeSalaryDeduction has no such
    // flag).
    public class SalaryLineInput
    {
        public string Code { get; set; }
        public AwardValueType ValueType { get; set; }
        public decimal Value { get; set; }
        public PayFrequencyType FrequencyType { get; set; }
        public bool IsTaxable { get; set; } = true;
        public bool IsRetirementContribution { get; set; }
    }
}
