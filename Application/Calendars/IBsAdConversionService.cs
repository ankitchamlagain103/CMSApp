namespace Application.Calendars
{
    // Bidirectional Gregorian (AD) <-> Bikram Sambat (BS) date conversion, driven by the
    // admin-editable BsMonthLength table (BS month lengths are set by yearly government
    // publication, not formula). All methods throw BsCalendarException when the requested
    // date is out of range or the required BS year configuration is missing.
    public interface IBsAdConversionService
    {
        Task<DateTime> ConvertBsToAdAsync(int bsYear, int bsMonth, int bsDay, CancellationToken cancellationToken = default);

        Task<(int BsYear, int BsMonth, int BsDay)> ConvertAdToBsAsync(DateTime adDate, CancellationToken cancellationToken = default);

        Task<int> GetDaysInBsMonthAsync(int bsYear, int bsMonth, CancellationToken cancellationToken = default);
    }
}
