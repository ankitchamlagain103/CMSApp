using Domain.Common;
using Domain.Common.Filters;
using Domain.Entities;

namespace Domain.Interfaces
{
    // Aggregate repository: PayrollRun plus its SalarySlip children and their SalarySlipLine
    // grandchildren.
    public interface IPayrollRunRepository : IRepository<PayrollRun, Guid>
    {
        Task<PagedResult<PayrollRun>> GetPagedByFilterAsync(PayrollRunFilter filter, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

        Task<PayrollRun> GetByIdWithSlipsAsync(Guid id, CancellationToken cancellationToken = default);

        // Whether a live (non-cancelled) run already exists for the fiscal month -- the
        // service-level twin of the partial unique index ix_payroll_runs_fiscal_month.
        Task<bool> ExistsForPeriodAsync(Guid fiscalYearId, int monthIndex, CancellationToken cancellationToken = default);

        // The live (non-cancelled) run for a fiscal month, headers only; null when the month
        // hasn't been generated.
        Task<PayrollRun> GetByPeriodAsync(Guid fiscalYearId, int monthIndex, CancellationToken cancellationToken = default);

        // Explicitly marks a new slip (and its untracked lines) as Added. Required on the
        // refresh path: a slip built with a pre-set Guid id and merely added to a tracked run's
        // collection would be picked up by change-tracker fixup as an EXISTING entity (set key
        // on a store-generated-key type => Unchanged), silently skipping its INSERT while its
        // id-less lines still insert -- an FK violation at save time.
        Task AddSlipAsync(SalarySlip slip, CancellationToken cancellationToken = default);

        Task<SalarySlip> GetSlipByIdAsync(Guid slipId, CancellationToken cancellationToken = default);

        // A persisted, Approved-or-Paid slip for (employee, fiscal year, month index) -- payslip
        // endpoints only ever surface payroll once it has been approved, never a Draft or a
        // read-time projection.
        Task<SalarySlip> GetSlipForPeriodAsync(Guid employeeId, Guid fiscalYearId, int monthIndex, CancellationToken cancellationToken = default);

        // Every Approved-or-Paid slip of one employee across a fiscal year, for the employee
        // payslip list -- months with no approved slip yet (no run, or still Draft) are omitted
        // entirely rather than shown as a projection.
        Task<IReadOnlyList<SalarySlip>> GetSlipsForYearAsync(Guid employeeId, Guid fiscalYearId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<string>> GetSlipNosByPrefixAsync(string prefix, CancellationToken cancellationToken = default);

        Task<SalarySlipLine> GetSlipLineByIdAsync(Guid lineId, CancellationToken cancellationToken = default);

        Task AddSlipLineAsync(SalarySlipLine line, CancellationToken cancellationToken = default);

        void RemoveSlipLine(SalarySlipLine line);
    }
}
