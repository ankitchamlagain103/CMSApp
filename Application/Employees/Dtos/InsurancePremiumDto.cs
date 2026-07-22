namespace Application.Employees.Dtos
{
    public class InsurancePremiumDto
    {
        public Guid Id { get; set; }
        public Guid EmployeeSalaryId { get; set; }
        public string InsuranceTypeCode { get; set; }

        // Human-readable InsuranceType catalog label (2026-07-19); falls back to the code when
        // the option no longer exists in the catalog.
        public string InsuranceTypeLabel { get; set; }
        public decimal AnnualPremiumAmount { get; set; }
    }
}
