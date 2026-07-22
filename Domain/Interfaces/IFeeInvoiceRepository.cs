using Domain.Common;
using Domain.Common.Filters;
using Domain.Entities;
using Domain.Enums;

namespace Domain.Interfaces
{
    // Aggregate repository: FeeInvoice plus its FeeInvoiceLine children, and the sibling
    // transactional records that only make sense next to invoices -- FeePayment (with its
    // FeePaymentAllocation children) and the pre-generation FeeAdjustment queue. One repository
    // because every payment/adjustment operation reads or mutates invoices in the same unit of
    // work (same aggregate-owns-children convention as ITeacherRepository/IEnrollmentRepository).
    public interface IFeeInvoiceRepository : IRepository<FeeInvoice, Guid>
    {
        Task<PagedResult<FeeInvoice>> GetPagedByFilterAsync(FeeInvoiceFilter filter, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

        Task<FeeInvoice> GetByIdWithLinesAsync(Guid id, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<FeeInvoice>> GetByIdsWithLinesAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken = default);

        // Every non-cancelled invoice of one enrollment, oldest billing period first -- the
        // statement/carry-forward view.
        Task<IReadOnlyList<FeeInvoice>> GetStatementByEnrollmentAsync(Guid enrollmentId, CancellationToken cancellationToken = default);

        // Invoices a payment can settle (Generated/Pending/PartiallyPaid), with lines, oldest
        // billing period first (FIFO allocation order).
        Task<IReadOnlyList<FeeInvoice>> GetOpenByEnrollmentAsync(Guid enrollmentId, CancellationToken cancellationToken = default);

        // Whether a live (non-cancelled) invoice already exists for the period -- the service-
        // level twin of the partial unique index ix_fee_invoices_enrollment_period.
        Task<bool> ExistsForPeriodAsync(Guid enrollmentId, int billingYear, int billingMonth, CancellationToken cancellationToken = default);

        // Every non-cancelled invoice (headers only) of the given enrollments, batched for a
        // generation run: existing-period detection, first-invoice detection (OneTime charges,
        // annual-installment anchoring), and the PreviousDueAmount snapshot in one query.
        Task<IReadOnlyList<FeeInvoice>> GetByEnrollmentIdsAsync(IReadOnlyList<Guid> enrollmentIds, CancellationToken cancellationToken = default);

        // With Lines included (2026-07-17) -- generation needs earlier invoices' actual
        // AnnualInstallment line amounts to compute an item's remaining balance (self-healing:
        // an early "pay in full" settlement is honored automatically, no separate flag needed).
        Task<IReadOnlyList<FeeInvoice>> GetByEnrollmentIdsWithLinesAsync(IReadOnlyList<Guid> enrollmentIds, CancellationToken cancellationToken = default);

        // Every non-cancelled invoice of one period (any scoped generate call, or advance
        // billing that happened to land in it), with Enrollment/ClassSection/AcademicClass
        // included (no Student -- the run list/summary views this backs never need a student
        // name, only the scalar StudentId and the class grouping) -- the FeeGenerationRun list
        // row and the run detail's per-class summary rollups.
        Task<IReadOnlyList<FeeInvoice>> GetByPeriodWithDetailsAsync(Guid academicYearId, int billingYear, int billingMonth, CancellationToken cancellationToken = default);

        // Same period filter narrowed to one class, with Student also included -- the
        // FeeGenerationRun class drill-down (student -> invoice breakdown) fetched separately
        // per class instead of bundled into the run detail response.
        Task<IReadOnlyList<FeeInvoice>> GetByPeriodForClassWithDetailsAsync(Guid academicYearId, int billingYear, int billingMonth, Guid academicClassId, CancellationToken cancellationToken = default);

        // Every invoice (any status -- includes the Cancelled/voided ones this exists to find)
        // whose CarriedForwardToInvoiceId points at the given invoice. Used when a Draft that
        // already absorbed a carry-forward gets regenerated: the voided invoice(s) need their
        // reference repointed from the replaced Draft's id to its replacement's id.
        Task<IReadOnlyList<FeeInvoice>> GetByCarriedForwardTargetAsync(Guid targetInvoiceId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<string>> GetInvoiceNosByPrefixAsync(string prefix, CancellationToken cancellationToken = default);

        Task<FeeInvoiceLine> GetLineByIdAsync(Guid lineId, CancellationToken cancellationToken = default);

        Task AddLineAsync(FeeInvoiceLine line, CancellationToken cancellationToken = default);

        void RemoveLine(FeeInvoiceLine line);

        Task<IReadOnlyList<FeeAdjustment>> GetAdjustmentsByFilterAsync(Guid? enrollmentId, int? billingYear, int? billingMonth, AdjustmentStatus? status, CancellationToken cancellationToken = default);

        Task<FeeAdjustment> GetAdjustmentByIdAsync(Guid adjustmentId, CancellationToken cancellationToken = default);

        // The Pending adjustments a generation run folds in, batched across the run's
        // enrollments.
        Task<IReadOnlyList<FeeAdjustment>> GetPendingAdjustmentsForPeriodAsync(IReadOnlyList<Guid> enrollmentIds, int billingYear, int billingMonth, CancellationToken cancellationToken = default);

        // Applied adjustments linked to these invoices -- reverted to Pending when their Draft
        // invoice is regenerated or cancelled.
        Task<IReadOnlyList<FeeAdjustment>> GetAdjustmentsAppliedToInvoicesAsync(IReadOnlyList<Guid> invoiceIds, CancellationToken cancellationToken = default);

        Task AddAdjustmentAsync(FeeAdjustment adjustment, CancellationToken cancellationToken = default);

        void RemoveAdjustment(FeeAdjustment adjustment);

        Task<PagedResult<FeePayment>> GetPagedPaymentsByFilterAsync(FeePaymentFilter filter, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

        // Every non-voided payment of one enrollment, oldest first -- the credit side of the
        // account-statement ledger.
        Task<IReadOnlyList<FeePayment>> GetPaymentsByEnrollmentAsync(Guid enrollmentId, CancellationToken cancellationToken = default);

        Task<FeePayment> GetPaymentByIdAsync(Guid paymentId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<string>> GetReceiptNosByPrefixAsync(string prefix, CancellationToken cancellationToken = default);

        Task AddPaymentAsync(FeePayment payment, CancellationToken cancellationToken = default);

        void RemoveAllocation(FeePaymentAllocation allocation);
    }
}
