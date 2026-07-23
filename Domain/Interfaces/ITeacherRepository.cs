using Domain.Common;
using Domain.Common.Filters;
using Domain.Entities;

namespace Domain.Interfaces
{
    // Aggregate repository: Teacher plus its TeacherAssignment children -- unchanged since the
    // Employee/Teacher split. Qualifications and Documents moved to IEmployeeRepository entirely
    // on 2026-07-23 (see EmployeeQualification/EmployeeDocument). Identity fields (name/phone/
    // status/employee code/join date) and salary live on Employee, so
    // GetPagedByFilterAsync/GetByIdWithEmployeeAsync join across via the shared-PK Employee nav.
    public interface ITeacherRepository : IRepository<Teacher, Guid>
    {
        Task<PagedResult<Teacher>> GetPagedByFilterAsync(TeacherFilter filter, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

        Task<Teacher> GetByIdWithEmployeeAsync(Guid id, CancellationToken cancellationToken = default);

        Task<bool> HasAssignmentsAsync(Guid teacherId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<TeacherAssignment>> GetAssignmentsAsync(Guid teacherId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<TeacherAssignment>> GetAssignmentsByClassSubjectIdsAsync(IReadOnlyCollection<Guid> classSubjectIds, CancellationToken cancellationToken = default);

        Task<TeacherAssignment> GetAssignmentByIdAsync(Guid assignmentId, CancellationToken cancellationToken = default);

        Task<bool> AssignmentExistsAsync(Guid teacherId, Guid classSubjectId, Guid? classSectionId, CancellationToken cancellationToken = default);

        Task<bool> ClassTeacherExistsForSectionAsync(Guid classSectionId, CancellationToken cancellationToken = default);

        Task AddAssignmentAsync(TeacherAssignment assignment, CancellationToken cancellationToken = default);

        void RemoveAssignment(TeacherAssignment assignment);
    }
}
