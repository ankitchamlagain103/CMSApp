using Domain.Enums;

namespace Application.Employees.Commands
{
    public class SalaryDeductionInput
    {
        public string DeductionCode { get; set; }
        public AwardValueType ValueType { get; set; }
        public decimal Value { get; set; }
        public PayFrequencyType FrequencyType { get; set; }
        public bool IsRetirementContribution { get; set; }
    }
}
