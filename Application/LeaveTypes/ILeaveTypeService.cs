using Application.Common.Models;
using Application.LeaveTypes.Commands;
using Application.LeaveTypes.Dtos;

namespace Application.LeaveTypes
{
    public interface ILeaveTypeService
    {
        Task<CommonResponse<LeaveTypeDto>> CreateLeaveTypeAsync(CreateLeaveTypeCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<LeaveTypeDto>> GetLeaveTypeByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<CommonResponse<PaginatedResponse<LeaveTypeDto>>> GetLeaveTypesAsync(int page, int pageSize, CancellationToken cancellationToken = default);

        Task<CommonResponse<LeaveTypeDto>> UpdateLeaveTypeAsync(Guid id, UpdateLeaveTypeCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> DeleteLeaveTypeAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
