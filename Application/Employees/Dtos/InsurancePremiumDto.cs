namespace Application.Employees.Dtos
{
    public class InsurancePremiumDto
    {
        public Guid Id { get; set; }
        public Guid EmployeeSalaryId { get; set; }
        public string InsuranceTypeCode { get; set; }
        public decimal AnnualPremiumAmount { get; set; }
    }
}
