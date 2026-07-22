namespace Application.Employees.Dtos
{
    // Outcome of a bulk salary-adjustment entry: how many Pending adjustments were created and
    // who was skipped (unknown id, filtered out, ...). Adjustments lists the created rows so
    // the UI can render/undo them without a follow-up query.
    public class BulkSalaryAdjustmentResultDto
    {
        public Guid FiscalYearId { get; set; }
        public int MonthIndex { get; set; }
        public string AdjustmentTypeCode { get; set; }
        public int CreatedCount { get; set; }
        public List<SalaryAdjustmentDto> Adjustments { get; set; } = new List<SalaryAdjustmentDto>();
        public List<BulkSalaryAdjustmentSkipDto> Skipped { get; set; } = new List<BulkSalaryAdjustmentSkipDto>();
    }
}
