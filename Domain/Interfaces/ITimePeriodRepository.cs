using Domain.Entities;

namespace Domain.Interfaces
{
    // Master-data repository for TimePeriod, plus the ClassTimePeriod mapping it owns (same
    // "aggregate repository owns its children" convention as IAcademicClassRepository owning
    // ClassSection/ClassSubject).
    public interface ITimePeriodRepository : IRepository<TimePeriod, Guid>
    {
        Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default);

        Task<bool> IsReferencedAsync(Guid timePeriodId, CancellationToken cancellationToken = default);

        // --- Class <-> TimePeriod mapping ---

        Task<List<ClassTimePeriod>> GetMappingsByClassIdAsync(Guid academicClassId, CancellationToken cancellationToken = default);

        Task<bool> IsMappedToClassAsync(Guid academicClassId, Guid timePeriodId, CancellationToken cancellationToken = default);

        Task AddMappingAsync(ClassTimePeriod mapping, CancellationToken cancellationToken = default);

        Task<ClassTimePeriod> GetMappingAsync(Guid academicClassId, Guid timePeriodId, CancellationToken cancellationToken = default);

        void RemoveMapping(ClassTimePeriod mapping);
    }
}
