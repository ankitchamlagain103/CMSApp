using Domain.Entities;

namespace Domain.Interfaces
{
    // Master-data repository, same shape as IAcademicYearRepository -- no line-item children to
    // own.
    public interface ILeaveTypeRepository : IRepository<LeaveType, Guid>
    {
        Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default);

        Task<bool> IsReferencedAsync(Guid leaveTypeId, CancellationToken cancellationToken = default);
    }
}
