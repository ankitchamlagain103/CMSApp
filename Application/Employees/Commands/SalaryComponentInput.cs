using Domain.Enums;

namespace Application.Employees.Commands
{
    // Used both as a nested item on AddEmployeeSalaryCommand (creating a full revision) and as the
    // standalone body for adding one component to an existing revision.
    public class SalaryComponentInput
    {
        public string ComponentCode { get; set; }
        public AwardValueType ValueType { get; set; }
        public decimal Value { get; set; }
        public PayFrequencyType FrequencyType { get; set; }
        public bool IsTaxable { get; set; } = true;
        public bool IsRetirementContribution { get; set; }
    }
}
