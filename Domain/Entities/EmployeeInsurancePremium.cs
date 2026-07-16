namespace Domain.Entities
{
    // An annual insurance premium (life/health/housing) declared for tax-deduction purposes.
    // InsuranceTypeCode is a Config code (ConfigTypeCodes.InsuranceType); each type's Config row
    // carries its Nepal tax-deduction cap in AdditionalValue1 (same Config-extensibility trick as
    // the Subject catalog's short-name/credit/category convention) -- TaxCalculator looks it up
    // and deducts Math.Min(AnnualPremiumAmount, cap) per premium. Hard-deleted (pure line item).
    public class EmployeeInsurancePremium : AuditableEntity
    {
        public Guid Id { get; set; }
        public Guid EmployeeSalaryId { get; set; }
        public string InsuranceTypeCode { get; set; }
        public decimal AnnualPremiumAmount { get; set; }
        public virtual EmployeeSalary EmployeeSalary { get; set; }
    }
}
