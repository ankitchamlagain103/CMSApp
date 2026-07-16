using Domain.Enums;

namespace Application.Employees.Dtos
{
    public class SalaryComponentDto
    {
        public Guid Id { get; set; }
        public Guid EmployeeSalaryId { get; set; }
        public string ComponentCode { get; set; }
        public AwardValueType ValueType { get; set; }
        public decimal Value { get; set; }
        public PayFrequencyType FrequencyType { get; set; }
        public bool IsTaxable { get; set; }
        public bool IsRetirementContribution { get; set; }
    }
}
