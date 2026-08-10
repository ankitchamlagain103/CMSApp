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

        Task<CommonResponse<ClassSubjectDto>> UpdateSubjectAsync(Guid academicClassId, Guid classSubjectId, UpdateClassSubjectCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> RemoveSubjectAsync(Guid academicClassId, Guid classSubjectId, CancellationToken cancellationToken = default);

        Task<CommonResponse<List<ClassSubjectDto>>> GetClassSubjectsAsync(Guid academicClassId, Guid? classSectionId, CancellationToken cancellationToken = default);

        // Class-scoped counterpart to IEmployeeService.AssignClassSubjectBulkEntryAsync -- that one
        // is scoped to one teacher and lets each row name its own class/subject/section/period;
        // this one is scoped to one AcademicClass (the id here) and lets each row name its own
        // teacher, so "who teaches this class" can be mapped in a single submission from the
        // class's own page instead of visiting every teacher's profile in turn.
        Task<CommonResponse<ClassTeacherAssignmentBulkEntryResultDto>> AssignTeachersBulkEntryAsync(Guid academicClassId, AssignClassTeachersBulkEntryCommand command, CancellationToken cancellationToken = default);

        // Read-side counterpart to AssignTeachersBulkEntryAsync -- "who teaches this class,"
        // listing every existing TeacherAssignment row for the class (optionally narrowed to one
        // section) instead of creating new ones. There was previously no GET endpoint for this --
        // only the bulk-create POST existed.
        Task<CommonResponse<List<ClassTeacherAssignmentDto>>> GetTeacherAssignmentsAsync(Guid academicClassId, Guid? classSectionId, CancellationToken cancellationToken = default);
    }
}
