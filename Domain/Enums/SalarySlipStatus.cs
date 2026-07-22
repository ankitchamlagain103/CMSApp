namespace Domain.Enums
{
    // Normally follows the owning PayrollRun's status; individually Cancellable while the run
    // is still unpaid (e.g. one resigned employee inside an otherwise-valid run).
    public enum SalarySlipStatus
    {
        Draft = 1,
        Approved = 2,
        Paid = 3,
        Cancelled = 4
    }
}
