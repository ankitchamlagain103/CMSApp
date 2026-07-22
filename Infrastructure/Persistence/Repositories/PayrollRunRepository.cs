using Domain.Common;
using Domain.Common.Filters;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories
{
    public class PayrollRunRepository : Repository<PayrollRun, Guid>, IPayrollRunRepository
    {
        public PayrollRunRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<PagedResult<PayrollRun>> GetPagedByFilterAsync(PayrollRunFilter filter, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            // Slips are included (headers only, no Lines) because the list DTO's
            // SlipCount/TotalGrossEarnings/TotalNetPay aggregates are computed from them --
            // without this Include every list row showed 0 slips / 0.00 totals.
            IQueryable<PayrollRun> runsQuery = DbSet
                .Include(r => r.FiscalYear)
                .Include(r => r.Slips);

            if (filter.FiscalYearId.HasValue)
            {
                runsQuery = runsQuery.Where(r => r.FiscalYearId == filter.FiscalYearId.Value);
            }

            if (filter.Status.HasValue)
            {
                runsQuery = runsQuery.Where(r => r.Status == filter.Status.Value);
            }

            var totalCount = await runsQuery.CountAsync(cancellationToken);
            var skipCount = (pageNumber - 1) * pageSize;
            var items = await runsQuery
                .OrderByDescending(r => r.FiscalYear.StartDate)
                .ThenByDescending(r => r.MonthIndex)
                .Skip(skipCount)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var pagedResult = new PagedResult<PayrollRun>
            {
                Items = items,
                TotalCount = totalCount
            };

            return pagedResult;
        }

        public async Task<PayrollRun> GetByIdWithSlipsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var run = await DbSet
                .Include(r => r.FiscalYear)
                .Include(r => r.Slips)
                    .ThenInclude(slip => slip.Employee)
                .Include(r => r.Slips)
                    .ThenInclude(slip => slip.Lines)
                .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

            return run;
        }

        public async Task<bool> ExistsForPeriodAsync(Guid fiscalYearId, int monthIndex, CancellationToken cancellationToken = default)
        {
            var exists = await DbSet
                .AnyAsync(r => r.FiscalYearId == fiscalYearId
                    && r.MonthIndex == monthIndex
                    && r.Status != PayrollRunStatus.Cancelled, cancellationToken);

            return exists;
        }

        public async Task<PayrollRun> GetByPeriodAsync(Guid fiscalYearId, int monthIndex, CancellationToken cancellationToken = default)
        {
            var run = await DbSet
                .FirstOrDefaultAsync(r => r.FiscalYearId == fiscalYearId
                    && r.MonthIndex == monthIndex
                    && r.Status != PayrollRunStatus.Cancelled, cancellationToken);

            return run;
        }

        public async Task<SalarySlip> GetSlipByIdAsync(Guid slipId, CancellationToken cancellationToken = default)
        {
            var slip = await DbContext.Set<SalarySlip>()
                .Include(s => s.Employee)
                .Include(s => s.Lines)
                .Include(s => s.PayrollRun)
                    .ThenInclude(run => run.FiscalYear)
                .FirstOrDefaultAsync(s => s.Id == slipId, cancellationToken);

            return slip;
        }

        public async Task<SalarySlip> GetSlipForPeriodAsync(Guid employeeId, Guid fiscalYearId, int monthIndex, CancellationToken cancellationToken = default)
        {
            var slip = await DbContext.Set<SalarySlip>()
                .Include(s => s.Employee)
                .Include(s => s.Lines)
                .Include(s => s.PayrollRun)
                    .ThenInclude(run => run.FiscalYear)
                .FirstOrDefaultAsync(s => s.EmployeeId == employeeId
                    && s.PayrollRun.FiscalYearId == fiscalYearId
                    && s.PayrollRun.MonthIndex == monthIndex
                    && (s.Status == SalarySlipStatus.Approved || s.Status == SalarySlipStatus.Paid)
                    && s.PayrollRun.Status != PayrollRunStatus.Cancelled, cancellationToken);

            return slip;
        }

        public async Task<IReadOnlyList<SalarySlip>> GetSlipsForYearAsync(Guid employeeId, Guid fiscalYearId, CancellationToken cancellationToken = default)
        {
            var slips = await DbContext.Set<SalarySlip>()
                .Include(s => s.Lines)
                .Include(s => s.PayrollRun)
                .Where(s => s.EmployeeId == employeeId
                    && s.PayrollRun.FiscalYearId == fiscalYearId
                    && (s.Status == SalarySlipStatus.Approved || s.Status == SalarySlipStatus.Paid)
                    && s.PayrollRun.Status != PayrollRunStatus.Cancelled)
                .ToListAsync(cancellationToken);

            return slips;
        }

        public async Task<IReadOnlyList<string>> GetSlipNosByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
        {
            // IgnoreQueryFilters: soft-deleted slips keep their numbers reserved.
            var slipNos = await DbContext.Set<SalarySlip>()
                .IgnoreQueryFilters()
                .Where(s => s.SlipNo.StartsWith(prefix))
                .Select(s => s.SlipNo)
                .ToListAsync(cancellationToken);

            return slipNos;
        }

        public async Task<SalarySlipLine> GetSlipLineByIdAsync(Guid lineId, CancellationToken cancellationToken = default)
        {
            var line = await DbContext.Set<SalarySlipLine>()
                .FirstOrDefaultAsync(l => l.Id == lineId, cancellationToken);

            return line;
        }

        public async Task AddSlipAsync(SalarySlip slip, CancellationToken cancellationToken = default)
        {
            await DbContext.Set<SalarySlip>().AddAsync(slip, cancellationToken);
        }

        public async Task AddSlipLineAsync(SalarySlipLine line, CancellationToken cancellationToken = default)
        {
            await DbContext.Set<SalarySlipLine>().AddAsync(line, cancellationToken);
        }

        public void RemoveSlipLine(SalarySlipLine line)
        {
            DbContext.Set<SalarySlipLine>().Remove(line);
        }
    }
}
