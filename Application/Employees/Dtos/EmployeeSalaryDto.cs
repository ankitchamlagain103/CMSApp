using Domain.Enums;

namespace Application.Employees.Dtos
{
    public class EmployeeSalaryDto
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public DateTime EffectiveFromDate { get; set; }
        public TaxAssessmentType AssessmentType { get; set; }
        public List<SalaryComponentDto> Components { get; set; } = new List<SalaryComponentDto>();
        public List<SalaryDeductionDto> Deductions { get; set; } = new List<SalaryDeductionDto>();
        public List<InsurancePremiumDto> InsurancePremiums { get; set; } = new List<InsurancePremiumDto>();
    }
}
