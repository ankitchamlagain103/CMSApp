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

        // parentCode/search (2026-07-24): generalized cascading/searchable dropdown filter --
        // parentCode narrows to options whose AdditionalValue1 equals it (District by
        // ProvinceCode, LocalLevel by DistrictCode, or any future hierarchical catalog), search
        // does a case-insensitive Label match (the LocalLevel searchable-lookup case). Both null
        // behaves exactly like the unfiltered call every existing caller already makes.
        Task<CommonResponse<List<DropdownItemDto>>> GetConfigsByTypeCodeAsync(int typeCode, string parentCode = null, string search = null, CancellationToken cancellationToken = default);
    }
}
