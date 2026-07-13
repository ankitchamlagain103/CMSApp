using Application.AcademicClasses.Commands;
using Application.AcademicClasses.Dtos;
using Application.AcademicClasses.Queries;
using Application.Common.Models;

namespace Application.AcademicClasses
{
    public interface IAcademicClassService
    {
        Task<CommonResponse<AcademicClassDto>> CreateAcademicClassAsync(CreateAcademicClassCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<AcademicClassDto>> GetAcademicClassByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<CommonResponse<PaginatedResponse<AcademicClassDto>>> GetAcademicClassesAsync(GetAcademicClassesQuery query, CancellationToken cancellationToken = default);

        Task<CommonResponse<AcademicClassDto>> UpdateAcademicClassAsync(Guid id, UpdateAcademicClassCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> DeleteAcademicClassAsync(Guid id, CancellationToken cancellationToken = default);

        Task<CommonResponse<ClassSectionDto>> AddSectionAsync(Guid academicClassId, CreateClassSectionCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<ClassSectionDto>> UpdateSectionAsync(Guid academicClassId, Guid classSectionId, UpdateClassSectionCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> RemoveSectionAsync(Guid academicClassId, Guid classSectionId, CancellationToken cancellationToken = default);

        Task<CommonResponse<List<ClassSectionDto>>> GetSectionsAsync(Guid academicClassId, CancellationToken cancellationToken = default);

        Task<CommonResponse<ClassSubjectDto>> AssignSubjectAsync(Guid academicClassId, AssignClassSubjectCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> RemoveSubjectAsync(Guid academicClassId, Guid classSubjectId, CancellationToken cancellationToken = default);

        Task<CommonResponse<List<ClassSubjectDto>>> GetClassSubjectsAsync(Guid academicClassId, Guid? classSectionId, CancellationToken cancellationToken = default);
    }
}
