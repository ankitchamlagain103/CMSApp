using Application.Common.Models;
using Application.Payroll.FiscalYears.Commands;
using Application.Payroll.FiscalYears.Dtos;
using Application.Payroll.FiscalYears.Queries;

namespace Application.Payroll.FiscalYears
{
    public interface IFiscalYearService
    {
        Task<CommonResponse<FiscalYearDto>> CreateFiscalYearAsync(CreateFiscalYearCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<FiscalYearDto>> GetFiscalYearByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<CommonResponse<PaginatedResponse<FiscalYearDto>>> GetFiscalYearsAsync(GetFiscalYearsQuery query, CancellationToken cancellationToken = default);

        Task<CommonResponse<FiscalYearDto>> UpdateFiscalYearAsync(Guid id, UpdateFiscalYearCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> DeleteFiscalYearAsync(Guid id, CancellationToken cancellationToken = default);

        Task<CommonResponse<TaxSlabDto>> AddTaxSlabAsync(Guid fiscalYearId, CreateTaxSlabCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<TaxSlabDto>> UpdateTaxSlabAsync(Guid fiscalYearId, Guid taxSlabId, UpdateTaxSlabCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> RemoveTaxSlabAsync(Guid fiscalYearId, Guid taxSlabId, CancellationToken cancellationToken = default);

        Task<CommonResponse<List<TaxSlabDto>>> GetTaxSlabsAsync(Guid fiscalYearId, CancellationToken cancellationToken = default);
    }
}
