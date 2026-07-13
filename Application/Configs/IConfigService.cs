using Application.Common.Models;
using Application.Configs.Commands;
using Application.Configs.Dtos;
using Application.Configs.Queries;

namespace Application.Configs
{
    public interface IConfigService
    {
        Task<CommonResponse<ConfigTypeDto>> CreateConfigTypeAsync(CreateConfigTypeCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<PaginatedResponse<ConfigTypeDto>>> GetConfigTypesAsync(GetConfigTypesQuery query, CancellationToken cancellationToken = default);

        Task<CommonResponse<ConfigTypeDto>> GetConfigTypeByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<CommonResponse<ConfigTypeDto>> UpdateConfigTypeAsync(Guid id, UpdateConfigTypeCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> DeleteConfigTypeAsync(Guid id, CancellationToken cancellationToken = default);

        Task<CommonResponse<ConfigDto>> CreateConfigAsync(CreateConfigCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<ConfigDto>> GetConfigByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<CommonResponse<ConfigDto>> UpdateConfigAsync(Guid id, UpdateConfigCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> DeleteConfigAsync(Guid id, CancellationToken cancellationToken = default);

        Task<CommonResponse<List<DropdownItemDto>>> GetConfigsByTypeCodeAsync(int typeCode, CancellationToken cancellationToken = default);
    }
}
