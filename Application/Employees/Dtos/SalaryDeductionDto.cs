using Domain.Enums;

namespace Application.Employees.Dtos
{
    public class SalaryDeductionDto
    {
        public Guid Id { get; set; }
        public Guid EmployeeSalaryId { get; set; }
        public string DeductionCode { get; set; }

        // Human-readable DeductionType catalog label (2026-07-19); falls back to the code when
        // the option no longer exists in the catalog.
        public string DeductionLabel { get; set; }
        public AwardValueType ValueType { get; set; }
        public decimal Value { get; set; }
        public PayFrequencyType FrequencyType { get; set; }
        public bool IsRetirementContribution { get; set; }
    }
}
