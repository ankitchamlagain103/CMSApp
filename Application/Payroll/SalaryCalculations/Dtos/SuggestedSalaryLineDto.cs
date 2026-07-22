using Domain.Enums;

namespace Application.Payroll.SalaryCalculations.Dtos
{
    // One ready-to-use compensation-plan line the calculator suggests -- shaped exactly like the
    // component/deduction inputs of POST /api/employees/{id}/salaries so a UI can prefill the
    // Add Salary Revision form from the calculation result without any field mapping.
    public class SuggestedSalaryLineDto
    {
        public string Code { get; set; }
        public AwardValueType ValueType { get; set; }
        public decimal Value { get; set; }
        public PayFrequencyType FrequencyType { get; set; }
        public bool IsTaxable { get; set; }
        public bool IsRetirementContribution { get; set; }
    }
}
