using Domain.Common;
using Domain.Common.Filters;
using Domain.Entities;

namespace Domain.Interfaces
{
    // Aggregate repository: LeaveRequest plus its LeaveSubstitute children (same shape as
    // IAcademicClassRepository owning ClassSection/ClassSubject).
    public interface ILeaveRequestRepository : IRepository<LeaveRequest, Guid>
    {
        Task<PagedResult<LeaveRequest>> GetPagedByFilterAsync(LeaveRequestFilter filter, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

        // Employee/LeaveType/SubstituteEmployee/Substitutes (with each substitute's own Employee)
        // all loaded -- for the single-request detail view and right before/after a decision is
        // recorded, so the returned DTO is fully resolved in one round trip.
        Task<LeaveRequest> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

        // Overlap check for the same employee/leave type -- FromDate/ToDate ranges that intersect
        // an existing non-Rejected request, excluding the request itself on update-like checks.
        Task<bool> HasOverlappingRequestAsync(Guid employeeId, DateTime fromDate, DateTime toDate, Guid? excludeRequestId, CancellationToken cancellationToken = default);

        // Policy caps (2026-07-24): every non-Rejected request of the same employee/leave type
        // whose FromDate/ToDate range overlaps [rangeStart, rangeEnd] -- the caller (EmployeeService)
        // clips each row's day count to the window and sums them for the MaxDaysPerWeek/
        // MaxDaysPerMonth checks. excludeRequestId mirrors HasOverlappingRequestAsync's param for
        // a future update-leave-request flow; always null today.
        Task<IReadOnlyList<LeaveRequest>> GetActiveRequestsInRangeAsync(Guid employeeId, Guid leaveTypeId, DateTime rangeStart, DateTime rangeEnd, Guid? excludeRequestId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<LeaveSubstitute>> GetSubstitutesAsync(Guid leaveRequestId, CancellationToken cancellationToken = default);

        Task AddSubstituteAsync(LeaveSubstitute substitute, CancellationToken cancellationToken = default);

        void RemoveSubstitute(LeaveSubstitute substitute);
    }
}
