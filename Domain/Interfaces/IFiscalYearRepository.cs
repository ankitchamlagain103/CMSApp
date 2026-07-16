using Domain.Common;
using Domain.Entities;
using Domain.Enums;

namespace Domain.Interfaces
{
    // Aggregate repository: FiscalYear plus its TaxSlab children (same shape as
    // IAcademicClassRepository owning ClassSection/ClassSubject).
    public interface IFiscalYearRepository : IRepository<FiscalYear, Guid>
    {
        Task<PagedResult<FiscalYear>> GetPagedOrderedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);

        Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<FiscalYear>> GetCurrentYearsAsync(CancellationToken cancellationToken = default);

        Task<FiscalYear> GetCurrentYearAsync(CancellationToken cancellationToken = default);

        Task<bool> HasTaxSlabsAsync(Guid fiscalYearId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<TaxSlab>> GetTaxSlabsAsync(Guid fiscalYearId, TaxAssessmentType? assessmentType, CancellationToken cancellationToken = default);

        Task<TaxSlab> GetTaxSlabByIdAsync(Guid taxSlabId, CancellationToken cancellationToken = default);

        Task AddTaxSlabAsync(TaxSlab taxSlab, CancellationToken cancellationToken = default);

        void RemoveTaxSlab(TaxSlab taxSlab);
    }
}
