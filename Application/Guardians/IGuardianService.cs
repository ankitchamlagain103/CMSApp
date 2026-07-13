using Application.Common.Models;
using Application.Guardians.Commands;
using Application.Guardians.Dtos;

namespace Application.Guardians
{
    public interface IGuardianService
    {
        Task<CommonResponse<GuardianDto>> CreateGuardianAsync(CreateGuardianCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<GuardianDto>> GetGuardianByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<CommonResponse<PaginatedResponse<GuardianDto>>> GetGuardiansAsync(int page, int pageSize, CancellationToken cancellationToken = default);

        Task<CommonResponse<GuardianDto>> UpdateGuardianAsync(Guid id, UpdateGuardianCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> DeleteGuardianAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
