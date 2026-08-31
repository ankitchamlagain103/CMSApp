using Domain.Common;
using Domain.Entities;

namespace Domain.Interfaces
{
    // Standalone master-data-shaped repository, same precedent as IGradeScaleRepository -- no
    // child link entities to own. StudentPromotion rows are immutable audit records (see the
    // doc comment on the entity), so only Add + read methods are ever called; the base
    // IRepository's Update/Remove exist for interface-shape consistency but PromotionService
    // never invokes them.
    public interface IStudentPromotionRepository : IRepository<StudentPromotion, Guid>
    {
        // Guards against promoting the same FromEnrollment twice (a student already moved on
        // from that enrollment) -- checked before a new StudentPromotion row is created.
        Task<bool> HasPromotionFromEnrollmentAsync(Guid fromEnrollmentId, CancellationToken cancellationToken = default);

        Task<PagedResult<StudentPromotion>> GetPagedByFilterAsync(Guid? studentId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

        Task<StudentPromotion> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
