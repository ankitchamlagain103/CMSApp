using Domain.Enums;

namespace Domain.Entities
{
    // One row per salary revision (a raise = a new row with a later EffectiveFromDate) -- salary
    // history is free the same way ClassSubject/FeeStructure get year-over-year history for free.
    // "Current" salary = the latest row by EffectiveFromDate. AssessmentType picks which TaxSlab
    // set (Individual/Couple) applies when computing tax. FKs to Employee (not Teacher) -- this is
    // what lets any staff member be paid, not just teachers. Replaces the old flat
    // BasicSalary/Allowances columns with real line items (Components/Deductions/
    // InsurancePremiums) so the compensation plan can represent named, school-configurable pay
    // items instead of two hardcoded numbers.
    public class EmployeeSalary : SoftDeleteAuditableEntity
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public DateTime EffectiveFromDate { get; set; }
        public TaxAssessmentType AssessmentType { get; set; }

        public virtual Employee Employee { get; set; }
        public virtual ICollection<EmployeeSalaryComponent> Components { get; set; } = new List<EmployeeSalaryComponent>();
        public virtual ICollection<EmployeeSalaryDeduction> Deductions { get; set; } = new List<EmployeeSalaryDeduction>();
        public virtual ICollection<EmployeeInsurancePremium> InsurancePremiums { get; set; } = new List<EmployeeInsurancePremium>();
    }
}
