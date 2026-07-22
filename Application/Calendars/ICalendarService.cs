using Application.Calendars.Commands;
using Application.Calendars.Dtos;
using Application.Calendars.Queries;
using Application.Common.Models;

namespace Application.Calendars
{
    public interface ICalendarService
    {
        // --- BS calendar configuration & localization ---

        Task<CommonResponse<List<BsMonthLengthDto>>> UpsertBsMonthLengthsAsync(UpsertBsMonthLengthsCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<List<BsMonthLengthDto>>> GetBsMonthLengthsAsync(int? bsYear, CancellationToken cancellationToken = default);

        Task<CommonResponse<CalendarLocalizationDto>> GetLocalizationDataAsync(CancellationToken cancellationToken = default);

        Task<CommonResponse<BsWeekdayNameDto>> UpdateWeekdayAsync(int weekdayIndex, UpdateBsWeekdayCommand command, CancellationToken cancellationToken = default);

        // --- Month view & conversion utilities ---

        Task<CommonResponse<CalendarMonthViewDto>> GetMonthViewAsync(GetMonthViewQuery query, CancellationToken cancellationToken = default);

        Task<CommonResponse<DualDateDto>> GetTodayAsync(CancellationToken cancellationToken = default);

        Task<CommonResponse<DualDateDto>> ConvertAdToBsAsync(DateTime adDate, CancellationToken cancellationToken = default);

        Task<CommonResponse<DualDateDto>> ConvertBsToAdAsync(int bsYear, int bsMonth, int bsDay, CancellationToken cancellationToken = default);

        // --- Calendar events (notes / public holidays / internal events) ---

        Task<CommonResponse<CalendarEventDto>> CreateCalendarEventAsync(CreateCalendarEventCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<CalendarEventDto>> GetCalendarEventByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<CommonResponse<PaginatedResponse<CalendarEventDto>>> GetCalendarEventsAsync(GetCalendarEventsQuery query, CancellationToken cancellationToken = default);

        Task<CommonResponse<CalendarEventDto>> UpdateCalendarEventAsync(Guid id, UpdateCalendarEventCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> DeleteCalendarEventAsync(Guid id, CancellationToken cancellationToken = default);

        // --- Festival occurrences (BS-anchored shifting festivals) ---

        Task<CommonResponse<FestivalOccurrenceDto>> CreateFestivalAsync(CreateFestivalOccurrenceCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<FestivalOccurrenceDto>> GetFestivalByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<CommonResponse<List<FestivalOccurrenceDto>>> GetFestivalsAsync(int? bsYear, bool? isActive, CancellationToken cancellationToken = default);

        Task<CommonResponse<FestivalOccurrenceDto>> UpdateFestivalAsync(Guid id, UpdateFestivalOccurrenceCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> DeleteFestivalAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
