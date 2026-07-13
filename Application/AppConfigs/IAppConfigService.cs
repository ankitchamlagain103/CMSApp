using Application.AppConfigs.Commands;
using Application.AppConfigs.Dtos;
using Application.AppConfigs.Queries;
using Application.Common.Models;

namespace Application.AppConfigs
{
    public interface IAppConfigService
    {
        Task<CommonResponse<AppConfigDto>> CreateAppConfigAsync(CreateAppConfigCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<PaginatedResponse<AppConfigDto>>> GetAppConfigsAsync(GetAppConfigsQuery query, CancellationToken cancellationToken = default);

        Task<CommonResponse<AppConfigDto>> GetAppConfigByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<CommonResponse<List<AppConfigDto>>> GetAppConfigsByGroupAsync(string configGroup, CancellationToken cancellationToken = default);

        Task<CommonResponse<AppConfigDto>> UpdateAppConfigAsync(Guid id, UpdateAppConfigCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> DeleteAppConfigAsync(Guid id, CancellationToken cancellationToken = default);

        Task<CommonResponse<List<PublicAppConfigDto>>> GetPublicAppConfigsAsync(CancellationToken cancellationToken = default);
    }
}
