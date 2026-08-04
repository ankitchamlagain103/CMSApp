using Application.Common.Models;
using Application.TimePeriods.Commands;
using Application.TimePeriods.Dtos;

namespace Application.TimePeriods
{
    public interface ITimePeriodService
    {
        Task<CommonResponse<TimePeriodDto>> CreateTimePeriodAsync(CreateTimePeriodCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<TimePeriodDto>> GetTimePeriodByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<CommonResponse<PaginatedResponse<TimePeriodDto>>> GetTimePeriodsAsync(int page, int pageSize, CancellationToken cancellationToken = default);

        Task<CommonResponse<TimePeriodDto>> UpdateTimePeriodAsync(Guid id, UpdateTimePeriodCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> DeleteTimePeriodAsync(Guid id, CancellationToken cancellationToken = default);

        // Bulk class<->period mapping -- the cross-product of every AcademicClassId x every
        // TimePeriodId in the command, skip-list style.
        Task<CommonResponse<ClassTimePeriodMapResultDto>> MapClassTimePeriodsAsync(MapClassTimePeriodsCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<List<ClassTimePeriodDto>>> GetClassTimePeriodsAsync(Guid academicClassId, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> UnmapClassTimePeriodAsync(Guid academicClassId, Guid timePeriodId, CancellationToken cancellationToken = default);
    }
}
