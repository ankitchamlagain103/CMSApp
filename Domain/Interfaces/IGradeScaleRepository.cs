using Domain.Entities;

namespace Domain.Interfaces
{
    // Master-data repository, same shape as ILeaveTypeRepository -- no line-item children to own,
    // and nothing FKs to a GradeScale row (its Grade/GradePoint values are copied, not
    // referenced -- see the doc comment on the entity), so no reference-check delete guard either.
    public interface IGradeScaleRepository : IRepository<GradeScale, Guid>
    {
        Task<bool> GradeExistsAsync(string grade, Guid? excludeGradeScaleId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<GradeScale>> GetAllOrderedAsync(CancellationToken cancellationToken = default);

        // The step-9 "Apply GradeScale" lookup -- the row whose [MinPercent, MaxPercent] band
        // contains the given percentage, or null if the configured scale has a gap.
        Task<GradeScale> FindByPercentageAsync(decimal percentage, CancellationToken cancellationToken = default);
    }
}
