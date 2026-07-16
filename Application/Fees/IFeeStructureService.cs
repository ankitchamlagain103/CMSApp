using Application.Common.Models;
using Application.Fees.Commands;
using Application.Fees.Dtos;
using Application.Fees.Queries;

namespace Application.Fees
{
    public interface IFeeStructureService
    {
        Task<CommonResponse<FeeStructureDto>> CreateFeeStructureAsync(CreateFeeStructureCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<FeeStructureDto>> GetFeeStructureByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<CommonResponse<PaginatedResponse<FeeStructureDto>>> GetFeeStructuresAsync(GetFeeStructuresQuery query, CancellationToken cancellationToken = default);

        Task<CommonResponse<FeeStructureDto>> UpdateFeeStructureAsync(Guid id, UpdateFeeStructureCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> DeleteFeeStructureAsync(Guid id, CancellationToken cancellationToken = default);

        Task<CommonResponse<FeeStructureItemDto>> AddItemAsync(Guid feeStructureId, FeeStructureItemInput command, CancellationToken cancellationToken = default);

        Task<CommonResponse<FeeStructureItemDto>> UpdateItemAsync(Guid feeStructureId, Guid itemId, UpdateFeeStructureItemCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> RemoveItemAsync(Guid feeStructureId, Guid itemId, CancellationToken cancellationToken = default);
    }
}
