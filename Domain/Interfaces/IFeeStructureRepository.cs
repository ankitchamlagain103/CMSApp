using Domain.Common;
using Domain.Common.Filters;
using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IFeeStructureRepository : IRepository<FeeStructure, Guid>
    {
        Task<PagedResult<FeeStructure>> GetPagedByFilterAsync(FeeStructureFilter filter, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

        // A class's full fee header + line items -- used by both the admin detail view and the
        // enrollment fee-structure computation. Null if the class has no fee structure yet.
        Task<FeeStructure> GetByAcademicClassIdAsync(Guid academicClassId, CancellationToken cancellationToken = default);

        // Same Include shape as GetByAcademicClassIdAsync, keyed by the header's own Id instead --
        // needed everywhere the header is looked up by its own id (detail/update/delete/item CRUD)
        // so Items/AcademicClass are populated instead of the base GetByIdAsync's bare entity.
        Task<FeeStructure> GetByIdWithItemsAsync(Guid id, CancellationToken cancellationToken = default);

        Task<bool> ExistsForAcademicClassAsync(Guid academicClassId, CancellationToken cancellationToken = default);

        Task<FeeStructureItem> GetItemByIdAsync(Guid itemId, CancellationToken cancellationToken = default);

        Task AddItemAsync(FeeStructureItem item, CancellationToken cancellationToken = default);

        void RemoveItem(FeeStructureItem item);
    }
}
