using Domain.Common;
using Domain.Common.Filters;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories
{
    public class FeeInvoiceRepository : Repository<FeeInvoice, Guid>, IFeeInvoiceRepository
    {
        public FeeInvoiceRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<PagedResult<FeeInvoice>> GetPagedByFilterAsync(FeeInvoiceFilter filter, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            IQueryable<FeeInvoice> invoicesQuery = DbSet
                .Include(i => i.Enrollment)
                    .ThenInclude(enrollment => enrollment.Student)
                .Include(i => i.Enrollment)
                    .ThenInclude(enrollment => enrollment.ClassSection)
                        .ThenInclude(section => section.AcademicClass);

            if (filter.AcademicYearId.HasValue)
            {
                invoicesQuery = invoicesQuery.Where(i => i.AcademicYearId == filter.AcademicYearId.Value);
            }

            if (filter.BillingYear.HasValue)
            {
                invoicesQuery = invoicesQuery.Where(i => i.BillingYear == filter.BillingYear.Value);
            }

            if (filter.BillingMonth.HasValue)
            {
                invoicesQuery = invoicesQuery.Where(i => i.BillingMonth == filter.BillingMonth.Value);
            }

            if (filter.AcademicClassId.HasValue)
            {
                invoicesQuery = invoicesQuery.Where(i => i.Enrollment.ClassSection.AcademicClassId == filter.AcademicClassId.Value);
            }

            if (filter.ClassSectionId.HasValue)
            {
                invoicesQuery = invoicesQuery.Where(i => i.Enrollment.ClassSectionId == filter.ClassSectionId.Value);
            }

            if (filter.EnrollmentId.HasValue)
            {
                invoicesQuery = invoicesQuery.Where(i => i.EnrollmentId == filter.EnrollmentId.Value);
            }

            if (filter.Status != null && filter.Status.Count > 0)
            {
                invoicesQuery = invoicesQuery.Where(i => filter.Status.Contains(i.Status));
            }

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var searchPattern = "%" + filter.Search.Trim() + "%";
                invoicesQuery = invoicesQuery.Where(i =>
                    EF.Functions.ILike(i.Enrollment.Student.FirstName, searchPattern)
                    || EF.Functions.ILike(i.Enrollment.Student.LastName, searchPattern)
                    || EF.Functions.ILike(i.Enrollment.Student.AdmissionNo, searchPattern)
                    || EF.Functions.ILike(i.Enrollment.Student.Email, searchPattern)
                    || EF.Functions.ILike(i.InvoiceNo, searchPattern));
            }

            var totalCount = await invoicesQuery.CountAsync(cancellationToken);
            var skipCount = (pageNumber - 1) * pageSize;
            var items = await invoicesQuery
                .OrderByDescending(i => i.BillingYear)
                .ThenByDescending(i => i.BillingMonth)
                .ThenBy(i => i.InvoiceNo)
                .Skip(skipCount)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var pagedResult = new PagedResult<FeeInvoice>
            {
                Items = items,
                TotalCount = totalCount
            };

            return pagedResult;
        }

        public async Task<FeeInvoice> GetByIdWithLinesAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var invoice = await DbSet
                .Include(i => i.Lines)
                .Include(i => i.Enrollment)
                    .ThenInclude(enrollment => enrollment.Student)
                .Include(i => i.Enrollment)
                    .ThenInclude(enrollment => enrollment.ClassSection)
                        .ThenInclude(section => section.AcademicClass)
                .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

            return invoice;
        }

        public async Task<IReadOnlyList<FeeInvoice>> GetByIdsWithLinesAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken = default)
        {
            var invoices = await DbSet
                .Include(i => i.Lines)
                .Where(i => ids.Contains(i.Id))
                .ToListAsync(cancellationToken);

            return invoices;
        }

        public async Task<IReadOnlyList<FeeInvoice>> GetStatementByEnrollmentAsync(Guid enrollmentId, CancellationToken cancellationToken = default)
        {
            var invoices = await DbSet
                .Include(i => i.Lines)
                .Where(i => i.EnrollmentId == enrollmentId && i.Status != FeeInvoiceStatus.Cancelled)
                .OrderBy(i => i.BillingYear)
                .ThenBy(i => i.BillingMonth)
                .ToListAsync(cancellationToken);

            return invoices;
        }

        public async Task<IReadOnlyList<FeeInvoice>> GetOpenByEnrollmentAsync(Guid enrollmentId, CancellationToken cancellationToken = default)
        {
            var openStatuses = new[] { FeeInvoiceStatus.Generated, FeeInvoiceStatus.Pending, FeeInvoiceStatus.PartiallyPaid };

            // A carried-forward invoice is voided (Status -> Cancelled, see
            // FeeInvoiceService.EnsureCarryForwardAdjustmentAsync) the moment its balance is
            // folded into a later invoice's CARRY_CORRECTION line, so openStatuses already
            // excludes it -- a payment settles the invoice that now represents that debt, not
            // this one a second time.
            var invoices = await DbSet
                .Include(i => i.Lines)
                .Where(i => i.EnrollmentId == enrollmentId && openStatuses.Contains(i.Status))
                .OrderBy(i => i.BillingYear)
                .ThenBy(i => i.BillingMonth)
                .ToListAsync(cancellationToken);

            return invoices;
        }

        public async Task<bool> ExistsForPeriodAsync(Guid enrollmentId, int billingYear, int billingMonth, CancellationToken cancellationToken = default)
        {
            var exists = await DbSet
                .AnyAsync(i => i.EnrollmentId == enrollmentId
                    && i.BillingYear == billingYear
                    && i.BillingMonth == billingMonth
                    && i.Status != FeeInvoiceStatus.Cancelled, cancellationToken);

            return exists;
        }

        public async Task<IReadOnlyList<FeeInvoice>> GetByEnrollmentIdsAsync(IReadOnlyList<Guid> enrollmentIds, CancellationToken cancellationToken = default)
        {
            var invoices = await DbSet
                .Where(i => enrollmentIds.Contains(i.EnrollmentId) && i.Status != FeeInvoiceStatus.Cancelled)
                .ToListAsync(cancellationToken);

            return invoices;
        }

        public async Task<IReadOnlyList<FeeInvoice>> GetByEnrollmentIdsWithLinesAsync(IReadOnlyList<Guid> enrollmentIds, CancellationToken cancellationToken = default)
        {
            // Ordered oldest-period-first: GenerateAsync's previousDue/annual-installment math
            // needs to walk invoices chronologically, not in arbitrary storage order.
            var invoices = await DbSet
                .Include(i => i.Lines)
                .Where(i => enrollmentIds.Contains(i.EnrollmentId) && i.Status != FeeInvoiceStatus.Cancelled)
                .OrderBy(i => i.BillingYear)
                .ThenBy(i => i.BillingMonth)
                .ToListAsync(cancellationToken);

            return invoices;
        }

        public async Task<IReadOnlyList<FeeInvoice>> GetByPeriodWithDetailsAsync(Guid academicYearId, int billingYear, int billingMonth, CancellationToken cancellationToken = default)
        {
            var invoices = await DbSet
                .Include(i => i.Enrollment)
                    .ThenInclude(enrollment => enrollment.ClassSection)
                        .ThenInclude(section => section.AcademicClass)
                .Where(i => i.AcademicYearId == academicYearId
                    && i.BillingYear == billingYear
                    && i.BillingMonth == billingMonth
                    && i.Status != FeeInvoiceStatus.Cancelled)
                .ToListAsync(cancellationToken);

            return invoices;
        }

        public async Task<IReadOnlyList<FeeInvoice>> GetByPeriodForClassWithDetailsAsync(Guid academicYearId, int billingYear, int billingMonth, Guid academicClassId, CancellationToken cancellationToken = default)
        {
            var invoices = await DbSet
                .Include(i => i.Enrollment)
                    .ThenInclude(enrollment => enrollment.Student)
                .Include(i => i.Enrollment)
                    .ThenInclude(enrollment => enrollment.ClassSection)
                        .ThenInclude(section => section.AcademicClass)
                .Where(i => i.AcademicYearId == academicYearId
                    && i.BillingYear == billingYear
                    && i.BillingMonth == billingMonth
                    && i.Status != FeeInvoiceStatus.Cancelled
                    && i.Enrollment.ClassSection.AcademicClassId == academicClassId)
                .ToListAsync(cancellationToken);

            return invoices;
        }

        public async Task<IReadOnlyList<FeeInvoice>> GetByCarriedForwardTargetAsync(Guid targetInvoiceId, CancellationToken cancellationToken = default)
        {
            var invoices = await DbSet
                .Where(i => i.CarriedForwardToInvoiceId == targetInvoiceId)
                .ToListAsync(cancellationToken);

            return invoices;
        }

        public async Task<IReadOnlyList<string>> GetInvoiceNosByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
        {
            // IgnoreQueryFilters: soft-deleted invoices keep their numbers reserved.
            var invoiceNos = await DbSet
                .IgnoreQueryFilters()
                .Where(i => i.InvoiceNo.StartsWith(prefix))
                .Select(i => i.InvoiceNo)
                .ToListAsync(cancellationToken);

            return invoiceNos;
        }

        public async Task<FeeInvoiceLine> GetLineByIdAsync(Guid lineId, CancellationToken cancellationToken = default)
        {
            var line = await DbContext.Set<FeeInvoiceLine>()
                .FirstOrDefaultAsync(l => l.Id == lineId, cancellationToken);

            return line;
        }

        public async Task AddLineAsync(FeeInvoiceLine line, CancellationToken cancellationToken = default)
        {
            await DbContext.Set<FeeInvoiceLine>().AddAsync(line, cancellationToken);
        }

        public void RemoveLine(FeeInvoiceLine line)
        {
            DbContext.Set<FeeInvoiceLine>().Remove(line);
        }

        public async Task<IReadOnlyList<FeeAdjustment>> GetAdjustmentsByFilterAsync(Guid? enrollmentId, int? billingYear, int? billingMonth, AdjustmentStatus? status, CancellationToken cancellationToken = default)
        {
            IQueryable<FeeAdjustment> adjustmentsQuery = DbContext.Set<FeeAdjustment>()
                .Include(a => a.Enrollment)
                    .ThenInclude(enrollment => enrollment.Student)
                .Include(a => a.Enrollment)
                    .ThenInclude(enrollment => enrollment.ClassSection)
                        .ThenInclude(section => section.AcademicClass);

            if (enrollmentId.HasValue)
            {
                adjustmentsQuery = adjustmentsQuery.Where(a => a.EnrollmentId == enrollmentId.Value);
            }

            if (billingYear.HasValue)
            {
                adjustmentsQuery = adjustmentsQuery.Where(a => a.BillingYear == billingYear.Value);
            }

            if (billingMonth.HasValue)
            {
                adjustmentsQuery = adjustmentsQuery.Where(a => a.BillingMonth == billingMonth.Value);
            }

            if (status.HasValue)
            {
                adjustmentsQuery = adjustmentsQuery.Where(a => a.Status == status.Value);
            }

            var adjustments = await adjustmentsQuery
                .OrderBy(a => a.BillingYear)
                .ThenBy(a => a.BillingMonth)
                .ThenBy(a => a.CreatedTs)
                .ToListAsync(cancellationToken);

            return adjustments;
        }

        public async Task<FeeAdjustment> GetAdjustmentByIdAsync(Guid adjustmentId, CancellationToken cancellationToken = default)
        {
            var adjustment = await DbContext.Set<FeeAdjustment>()
                .FirstOrDefaultAsync(a => a.Id == adjustmentId, cancellationToken);

            return adjustment;
        }

        public async Task<IReadOnlyList<FeeAdjustment>> GetPendingAdjustmentsForPeriodAsync(IReadOnlyList<Guid> enrollmentIds, int billingYear, int billingMonth, CancellationToken cancellationToken = default)
        {
            var adjustments = await DbContext.Set<FeeAdjustment>()
                .Where(a => enrollmentIds.Contains(a.EnrollmentId)
                    && a.BillingYear == billingYear
                    && a.BillingMonth == billingMonth
                    && a.Status == AdjustmentStatus.Pending)
                .ToListAsync(cancellationToken);

            return adjustments;
        }

        public async Task<IReadOnlyList<FeeAdjustment>> GetAdjustmentsAppliedToInvoicesAsync(IReadOnlyList<Guid> invoiceIds, CancellationToken cancellationToken = default)
        {
            var adjustments = await DbContext.Set<FeeAdjustment>()
                .Where(a => a.AppliedFeeInvoiceId != null && invoiceIds.Contains(a.AppliedFeeInvoiceId.Value))
                .ToListAsync(cancellationToken);

            return adjustments;
        }

        public async Task AddAdjustmentAsync(FeeAdjustment adjustment, CancellationToken cancellationToken = default)
        {
            await DbContext.Set<FeeAdjustment>().AddAsync(adjustment, cancellationToken);
        }

        public void RemoveAdjustment(FeeAdjustment adjustment)
        {
            DbContext.Set<FeeAdjustment>().Remove(adjustment);
        }

        public async Task<PagedResult<FeePayment>> GetPagedPaymentsByFilterAsync(FeePaymentFilter filter, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            IQueryable<FeePayment> paymentsQuery = DbContext.Set<FeePayment>()
                .Include(p => p.Enrollment)
                    .ThenInclude(enrollment => enrollment.Student)
                .Include(p => p.Allocations);

            if (filter.EnrollmentId.HasValue)
            {
                paymentsQuery = paymentsQuery.Where(p => p.EnrollmentId == filter.EnrollmentId.Value);
            }

            if (filter.FromDate.HasValue)
            {
                paymentsQuery = paymentsQuery.Where(p => p.PaymentDate >= filter.FromDate.Value);
            }

            if (filter.ToDate.HasValue)
            {
                paymentsQuery = paymentsQuery.Where(p => p.PaymentDate <= filter.ToDate.Value);
            }

            if (filter.PaymentMode.HasValue)
            {
                paymentsQuery = paymentsQuery.Where(p => p.PaymentMode == filter.PaymentMode.Value);
            }

            if (filter.Status.HasValue)
            {
                paymentsQuery = paymentsQuery.Where(p => p.Status == filter.Status.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var searchPattern = "%" + filter.Search.Trim() + "%";
                paymentsQuery = paymentsQuery.Where(p =>
                    EF.Functions.ILike(p.Enrollment.Student.FirstName, searchPattern)
                    || EF.Functions.ILike(p.Enrollment.Student.LastName, searchPattern)
                    || EF.Functions.ILike(p.Enrollment.Student.AdmissionNo, searchPattern)
                    || EF.Functions.ILike(p.Enrollment.Student.Email, searchPattern)
                    || EF.Functions.ILike(p.ReceiptNo, searchPattern));
            }

            var totalCount = await paymentsQuery.CountAsync(cancellationToken);
            var skipCount = (pageNumber - 1) * pageSize;
            var items = await paymentsQuery
                .OrderByDescending(p => p.PaymentDate)
                .ThenByDescending(p => p.ReceiptNo)
                .Skip(skipCount)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var pagedResult = new PagedResult<FeePayment>
            {
                Items = items,
                TotalCount = totalCount
            };

            return pagedResult;
        }

        public async Task<IReadOnlyList<FeePayment>> GetPaymentsByEnrollmentAsync(Guid enrollmentId, CancellationToken cancellationToken = default)
        {
            var payments = await DbContext.Set<FeePayment>()
                .Where(p => p.EnrollmentId == enrollmentId && p.Status != FeePaymentStatus.Voided)
                .OrderBy(p => p.PaymentDate)
                .ThenBy(p => p.ReceiptNo)
                .ToListAsync(cancellationToken);

            return payments;
        }

        public async Task<FeePayment> GetPaymentByIdAsync(Guid paymentId, CancellationToken cancellationToken = default)
        {
            var payment = await DbContext.Set<FeePayment>()
                .Include(p => p.Enrollment)
                    .ThenInclude(enrollment => enrollment.Student)
                .Include(p => p.Allocations)
                    .ThenInclude(allocation => allocation.FeeInvoice)
                .FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken);

            return payment;
        }

        public async Task<IReadOnlyList<string>> GetReceiptNosByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
        {
            // IgnoreQueryFilters: soft-deleted payments keep their receipt numbers reserved.
            var receiptNos = await DbContext.Set<FeePayment>()
                .IgnoreQueryFilters()
                .Where(p => p.ReceiptNo.StartsWith(prefix))
                .Select(p => p.ReceiptNo)
                .ToListAsync(cancellationToken);

            return receiptNos;
        }

        public async Task AddPaymentAsync(FeePayment payment, CancellationToken cancellationToken = default)
        {
            await DbContext.Set<FeePayment>().AddAsync(payment, cancellationToken);
        }

        public void RemoveAllocation(FeePaymentAllocation allocation)
        {
            DbContext.Set<FeePaymentAllocation>().Remove(allocation);
        }
    }
}
