using Domain.Enums;

namespace Application.Employees.Dtos
{
    // Unified read shape for AddSalaryLineAsync (2026-07-23) -- wraps whichever of
    // SalaryComponentDto/SalaryDeductionDto the submitted Code resolved to. IsTaxable is null when
    // CalculateType is Deduction (EmployeeSalaryDeduction has no such flag).
    public class SalaryLineDto
    {
        public Guid Id { get; set; }
        public Guid EmployeeSalaryId { get; set; }
        public string Code { get; set; }
        public string Label { get; set; }
        public string CalculateType { get; set; }
        public AwardValueType ValueType { get; set; }
        public decimal Value { get; set; }
        public PayFrequencyType FrequencyType { get; set; }
        public bool? IsTaxable { get; set; }
        public bool IsRetirementContribution { get; set; }
    }
}
