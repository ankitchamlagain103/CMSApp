using Application.AcademicYears.Commands;
using Application.AcademicYears.Dtos;
using Application.AcademicYears.Queries;
using Application.Common.Models;

namespace Application.AcademicYears
{
    public interface IAcademicYearService
    {
        Task<CommonResponse<AcademicYearDto>> CreateAcademicYearAsync(CreateAcademicYearCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<AcademicYearDto>> GetAcademicYearByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<CommonResponse<PaginatedResponse<AcademicYearDto>>> GetAcademicYearsAsync(GetAcademicYearsQuery query, CancellationToken cancellationToken = default);

        Task<CommonResponse<AcademicYearDto>> UpdateAcademicYearAsync(Guid id, UpdateAcademicYearCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> DeleteAcademicYearAsync(Guid id, CancellationToken cancellationToken = default);

        Task<CommonResponse<CloneStructureResultDto>> CloneStructureAsync(Guid targetAcademicYearId, CloneYearStructureCommand command, CancellationToken cancellationToken = default);
    }
}
