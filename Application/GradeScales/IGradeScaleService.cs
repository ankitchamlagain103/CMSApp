using Application.Common.Models;
using Application.GradeScales.Commands;
using Application.GradeScales.Dtos;

namespace Application.GradeScales
{
    public interface IGradeScaleService
    {
        Task<CommonResponse<GradeScaleDto>> CreateGradeScaleAsync(CreateGradeScaleCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<GradeScaleDto>> GetGradeScaleByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<CommonResponse<List<GradeScaleDto>>> GetGradeScalesAsync(CancellationToken cancellationToken = default);

        Task<CommonResponse<GradeScaleDto>> UpdateGradeScaleAsync(Guid id, UpdateGradeScaleCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> DeleteGradeScaleAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
