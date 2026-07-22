using Domain.Entities;

namespace Domain.Interfaces
{
    // Aggregate repository for the BS calendar reference/configuration tables. BsMonthLength
    // is the primary entity; the BsMonthName/BsWeekdayName localization rows are owned here
    // too (12 + 7 fixed rows, no repository of their own -- same "owner repo handles child
    // reference rows" convention as ITeacherRepository's qualifications).
    public interface ICalendarConfigRepository : IRepository<BsMonthLength, Guid>
    {
        Task<IReadOnlyList<BsMonthLength>> GetMonthLengthsAsync(int? bsYear = null, CancellationToken cancellationToken = default);

        Task<BsMonthLength> GetMonthLengthAsync(int bsYear, int bsMonth, CancellationToken cancellationToken = default);

        // The conversion engine's input: every configured row ordered by (BsYear, BsMonth).
        Task<IReadOnlyList<BsMonthLength>> GetAllMonthLengthsOrderedAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<BsMonthName>> GetMonthNamesAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<BsWeekdayName>> GetWeekdayNamesAsync(CancellationToken cancellationToken = default);

        Task<BsWeekdayName> GetWeekdayByIndexAsync(int weekdayIndex, CancellationToken cancellationToken = default);
    }
}
