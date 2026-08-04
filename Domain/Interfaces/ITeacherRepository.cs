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

        // 2026-08-04: class-scoped listing -- every TeacherAssignment whose ClassSubject belongs
        // to the given AcademicClass, optionally narrowed to one ClassSectionId. Backs "who
        // teaches this class" (GET /api/academicclasses/{id}/teacher-assignments), the read
        // counterpart to AcademicClassService.AssignTeachersBulkEntryAsync. Not reused from
        // GetAssignmentsByClassSubjectIdsAsync above -- that one's Include shape (Teacher/Employee
        // only) is tailored to its own caller; this one needs ClassSubject/ClassSection/TimePeriod
        // too, for a self-contained listing row.
        Task<IReadOnlyList<TeacherAssignment>> GetAssignmentsByAcademicClassAsync(Guid academicClassId, Guid? classSectionId, CancellationToken cancellationToken = default);

        Task<TeacherAssignment> GetAssignmentByIdAsync(Guid assignmentId, CancellationToken cancellationToken = default);

        Task<bool> AssignmentExistsAsync(Guid teacherId, Guid classSubjectId, Guid? classSectionId, CancellationToken cancellationToken = default);

        Task<bool> ClassTeacherExistsForSectionAsync(Guid classSectionId, CancellationToken cancellationToken = default);

        // 2026-08-04: a teacher can only be in one class/section during a given TimePeriod --
        // checked against every one of the teacher's OTHER assignments regardless of which
        // class/subject/section they're for, since sharing a TimePeriod row means sharing the
        // same wall-clock slot.
        Task<bool> TeacherHasTimePeriodConflictAsync(Guid teacherId, Guid timePeriodId, CancellationToken cancellationToken = default);

        Task AddAssignmentAsync(TeacherAssignment assignment, CancellationToken cancellationToken = default);

        void RemoveAssignment(TeacherAssignment assignment);
    }
}
