using Domain.Enums;

namespace Application.Employees.Commands
{
    // Creates one salary revision with its full compensation plan in a single call -- Components/
    // Deductions/InsurancePremiums are all created alongside the revision in one SaveChangesAsync.
    public class AddEmployeeSalaryCommand
    {
        public DateTime EffectiveFromDate { get; set; }
        public TaxAssessmentType AssessmentType { get; set; }
        public List<SalaryComponentInput> Components { get; set; } = new List<SalaryComponentInput>();
        public List<SalaryDeductionInput> Deductions { get; set; } = new List<SalaryDeductionInput>();
        public List<InsurancePremiumInput> InsurancePremiums { get; set; } = new List<InsurancePremiumInput>();
    }
}
