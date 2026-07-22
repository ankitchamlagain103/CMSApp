using Domain.Enums;

namespace Application.Employees.Dtos
{
    public class SalaryAdjustmentDto
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public Guid FiscalYearId { get; set; }
        public int MonthIndex { get; set; }
        public string AdjustmentTypeCode { get; set; }

        // Human-readable SalaryAdjustmentType catalog label (2026-07-19); falls back to the
        // code when the option no longer exists in the catalog.
        public string AdjustmentTypeLabel { get; set; }
        public AdjustmentDirection Direction { get; set; }
        public AwardValueType ValueType { get; set; }
        public decimal Value { get; set; }
        public decimal? Quantity { get; set; }
        public string Remarks { get; set; }
        public AdjustmentStatus Status { get; set; }
        public Guid? AppliedSalarySlipId { get; set; }
    }
}
