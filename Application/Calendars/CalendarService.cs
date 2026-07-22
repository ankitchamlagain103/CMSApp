using Application.Calendars.Commands;
using Application.Calendars.Dtos;
using Application.Calendars.Queries;
using Application.Calendars.Validators;
using Application.Common.Helpers;
using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Common.Filters;
using Domain.Entities;
using FluentValidation.Results;

namespace Application.Calendars
{
    public class CalendarService : ICalendarService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IBsAdConversionService _conversionService;
        private readonly UpsertBsMonthLengthsCommandValidator _upsertMonthLengthsValidator;
        private readonly UpdateBsWeekdayCommandValidator _updateWeekdayValidator;
        private readonly CreateCalendarEventCommandValidator _createEventValidator;
        private readonly UpdateCalendarEventCommandValidator _updateEventValidator;
        private readonly CreateFestivalOccurrenceCommandValidator _createFestivalValidator;
        private readonly UpdateFestivalOccurrenceCommandValidator _updateFestivalValidator;

        public CalendarService(
            IUnitOfWork unitOfWork,
            IBsAdConversionService conversionService,
            UpsertBsMonthLengthsCommandValidator upsertMonthLengthsValidator,
            UpdateBsWeekdayCommandValidator updateWeekdayValidator,
            CreateCalendarEventCommandValidator createEventValidator,
            UpdateCalendarEventCommandValidator updateEventValidator,
            CreateFestivalOccurrenceCommandValidator createFestivalValidator,
            UpdateFestivalOccurrenceCommandValidator updateFestivalValidator)
        {
            _unitOfWork = unitOfWork;
            _conversionService = conversionService;
            _upsertMonthLengthsValidator = upsertMonthLengthsValidator;
            _updateWeekdayValidator = updateWeekdayValidator;
            _createEventValidator = createEventValidator;
            _updateEventValidator = updateEventValidator;
            _createFestivalValidator = createFestivalValidator;
            _updateFestivalValidator = updateFestivalValidator;
        }

        // --- BS calendar configuration & localization ---

        public async Task<CommonResponse<List<BsMonthLengthDto>>> UpsertBsMonthLengthsAsync(UpsertBsMonthLengthsCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _upsertMonthLengthsValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<List<BsMonthLengthDto>>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var seenPairs = new HashSet<(int BsYear, int BsMonth)>();
            var duplicatePairs = new List<string>();
            foreach (var item in command.Items)
            {
                if (!seenPairs.Add((item.BsYear, item.BsMonth)))
                {
                    duplicatePairs.Add(item.BsYear + "-" + item.BsMonth);
                }
            }

            if (duplicatePairs.Count > 0)
            {
                var duplicateResponse = CommonResponse<List<BsMonthLengthDto>>.Fail(ResponseCodes.ValidationError, "Duplicate (BsYear, BsMonth) pairs in the payload: " + string.Join(", ", duplicatePairs) + ".");
                return duplicateResponse;
            }

            var affectedRows = new List<BsMonthLength>();
            foreach (var item in command.Items)
            {
                var existing = await _unitOfWork.CalendarConfigs.GetMonthLengthAsync(item.BsYear, item.BsMonth, cancellationToken);
                if (existing != null)
                {
                    existing.DaysInMonth = item.DaysInMonth;
                    _unitOfWork.CalendarConfigs.Update(existing);
                    affectedRows.Add(existing);
                }
                else
                {
                    var monthLength = new BsMonthLength
                    {
                        BsYear = item.BsYear,
                        BsMonth = item.BsMonth,
                        DaysInMonth = item.DaysInMonth
                    };

                    await _unitOfWork.CalendarConfigs.AddAsync(monthLength, cancellationToken);
                    affectedRows.Add(monthLength);
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var monthLengthDtos = new List<BsMonthLengthDto>();
            foreach (var row in affectedRows)
            {
                var monthLengthDto = CalendarMapper.ToDto(row);
                monthLengthDtos.Add(monthLengthDto);
            }

            var successResponse = CommonResponse<List<BsMonthLengthDto>>.Success(monthLengthDtos, "BS month-length configuration synchronized successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<List<BsMonthLengthDto>>> GetBsMonthLengthsAsync(int? bsYear, CancellationToken cancellationToken = default)
        {
            var monthLengths = await _unitOfWork.CalendarConfigs.GetMonthLengthsAsync(bsYear, cancellationToken);

            var monthLengthDtos = new List<BsMonthLengthDto>();
            foreach (var monthLength in monthLengths)
            {
                var monthLengthDto = CalendarMapper.ToDto(monthLength);
                monthLengthDtos.Add(monthLengthDto);
            }

            var successResponse = CommonResponse<List<BsMonthLengthDto>>.Success(monthLengthDtos);
            return successResponse;
        }

        public async Task<CommonResponse<CalendarLocalizationDto>> GetLocalizationDataAsync(CancellationToken cancellationToken = default)
        {
            var monthNames = await _unitOfWork.CalendarConfigs.GetMonthNamesAsync(cancellationToken);
            var weekdayNames = await _unitOfWork.CalendarConfigs.GetWeekdayNamesAsync(cancellationToken);

            var localizationDto = new CalendarLocalizationDto();
            foreach (var monthName in monthNames)
            {
                var monthNameDto = CalendarMapper.ToDto(monthName);
                localizationDto.Months.Add(monthNameDto);
            }

            foreach (var weekdayName in weekdayNames)
            {
                var weekdayNameDto = CalendarMapper.ToDto(weekdayName);
                localizationDto.Weekdays.Add(weekdayNameDto);
            }

            var successResponse = CommonResponse<CalendarLocalizationDto>.Success(localizationDto);
            return successResponse;
        }

        public async Task<CommonResponse<BsWeekdayNameDto>> UpdateWeekdayAsync(int weekdayIndex, UpdateBsWeekdayCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _updateWeekdayValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<BsWeekdayNameDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var weekday = await _unitOfWork.CalendarConfigs.GetWeekdayByIndexAsync(weekdayIndex, cancellationToken);
            if (weekday == null)
            {
                var notFoundResponse = CommonResponse<BsWeekdayNameDto>.Fail(ResponseCodes.NotFound, "Weekday with index '" + weekdayIndex + "' was not found (expected 0=Sunday to 6=Saturday).");
                return notFoundResponse;
            }

            weekday.NameEn = command.NameEn.Trim();
            weekday.NameNp = command.NameNp.Trim();
            weekday.IsWeeklyHoliday = command.IsWeeklyHoliday;

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var weekdayDto = CalendarMapper.ToDto(weekday);
            var successResponse = CommonResponse<BsWeekdayNameDto>.Success(weekdayDto, "Weekday updated successfully.");
            return successResponse;
        }

        // --- Month view & conversion utilities ---

        public async Task<CommonResponse<CalendarMonthViewDto>> GetMonthViewAsync(GetMonthViewQuery query, CancellationToken cancellationToken = default)
        {
            var mode = string.IsNullOrWhiteSpace(query.Mode) ? "BS" : query.Mode.Trim().ToUpperInvariant();
            if (mode != "BS" && mode != "AD")
            {
                var modeFailureResponse = CommonResponse<CalendarMonthViewDto>.Fail(ResponseCodes.ValidationError, "Mode must be 'BS' or 'AD'.");
                return modeFailureResponse;
            }

            if (query.Month < 1 || query.Month > 12)
            {
                var monthFailureResponse = CommonResponse<CalendarMonthViewDto>.Fail(ResponseCodes.ValidationError, "Month must be between 1 and 12.");
                return monthFailureResponse;
            }

            DateTime startAdDate;
            DateTime endAdDate;
            try
            {
                if (mode == "BS")
                {
                    var totalDays = await _conversionService.GetDaysInBsMonthAsync(query.Year, query.Month, cancellationToken);
                    startAdDate = await _conversionService.ConvertBsToAdAsync(query.Year, query.Month, 1, cancellationToken);
                    endAdDate = startAdDate.AddDays(totalDays - 1);
                }
                else
                {
                    if (query.Year < 1944 || query.Year > 2200)
                    {
                        var yearFailureResponse = CommonResponse<CalendarMonthViewDto>.Fail(ResponseCodes.ValidationError, "AD year must be between 1944 and 2200.");
                        return yearFailureResponse;
                    }

                    startAdDate = new DateTime(query.Year, query.Month, 1);
                    endAdDate = new DateTime(query.Year, query.Month, DateTime.DaysInMonth(query.Year, query.Month));
                }

                var monthViewDto = await BuildMonthViewAsync(mode, query.Year, query.Month, startAdDate, endAdDate, cancellationToken);
                var successResponse = CommonResponse<CalendarMonthViewDto>.Success(monthViewDto);
                return successResponse;
            }
            catch (BsCalendarException calendarException)
            {
                var conversionFailureResponse = CommonResponse<CalendarMonthViewDto>.Fail(ResponseCodes.ValidationError, calendarException.Message);
                return conversionFailureResponse;
            }
        }

        public async Task<CommonResponse<DualDateDto>> GetTodayAsync(CancellationToken cancellationToken = default)
        {
            var nepalToday = NepalDateHelper.GetNepalToday();
            var response = await ConvertAdToBsAsync(nepalToday, cancellationToken);
            return response;
        }

        public async Task<CommonResponse<DualDateDto>> ConvertAdToBsAsync(DateTime adDate, CancellationToken cancellationToken = default)
        {
            try
            {
                var dualDateDto = await BuildDualDateAsync(adDate.Date, cancellationToken);
                var successResponse = CommonResponse<DualDateDto>.Success(dualDateDto);
                return successResponse;
            }
            catch (BsCalendarException calendarException)
            {
                var failureResponse = CommonResponse<DualDateDto>.Fail(ResponseCodes.ValidationError, calendarException.Message);
                return failureResponse;
            }
        }

        public async Task<CommonResponse<DualDateDto>> ConvertBsToAdAsync(int bsYear, int bsMonth, int bsDay, CancellationToken cancellationToken = default)
        {
            try
            {
                var adDate = await _conversionService.ConvertBsToAdAsync(bsYear, bsMonth, bsDay, cancellationToken);
                var dualDateDto = await BuildDualDateAsync(adDate, cancellationToken);
                var successResponse = CommonResponse<DualDateDto>.Success(dualDateDto);
                return successResponse;
            }
            catch (BsCalendarException calendarException)
            {
                var failureResponse = CommonResponse<DualDateDto>.Fail(ResponseCodes.ValidationError, calendarException.Message);
                return failureResponse;
            }
        }

        // --- Calendar events ---

        public async Task<CommonResponse<CalendarEventDto>> CreateCalendarEventAsync(CreateCalendarEventCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _createEventValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<CalendarEventDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            DateTime adDate;
            int bsYear;
            int bsMonth;
            int bsDay;
            try
            {
                (adDate, bsYear, bsMonth, bsDay) = await ResolveDualDateAsync(command.IsBsDate, command.AdDate, command.BsYear, command.BsMonth, command.BsDay, cancellationToken);
            }
            catch (BsCalendarException calendarException)
            {
                var conversionFailureResponse = CommonResponse<CalendarEventDto>.Fail(ResponseCodes.ValidationError, calendarException.Message);
                return conversionFailureResponse;
            }

            var calendarEvent = new CalendarEvent
            {
                Title = command.Title.Trim(),
                EventType = command.EventType,
                AdDate = adDate,
                BsYear = bsYear,
                BsMonth = bsMonth,
                BsDay = bsDay,
                Description = command.Description,
                IconKey = command.IconKey,
                ColorCode = command.ColorCode,
                Language = string.IsNullOrWhiteSpace(command.Language) ? "en" : command.Language.Trim(),
                IsActive = command.IsActive
            };

            await _unitOfWork.CalendarEvents.AddAsync(calendarEvent, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var eventDto = CalendarMapper.ToDto(calendarEvent);
            var successResponse = CommonResponse<CalendarEventDto>.Success(eventDto, "Calendar event created successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<CalendarEventDto>> GetCalendarEventByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var calendarEvent = await _unitOfWork.CalendarEvents.GetByIdAsync(id, cancellationToken);
            if (calendarEvent == null)
            {
                var notFoundResponse = CommonResponse<CalendarEventDto>.Fail(ResponseCodes.NotFound, "Calendar event with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            var eventDto = CalendarMapper.ToDto(calendarEvent);
            var successResponse = CommonResponse<CalendarEventDto>.Success(eventDto);
            return successResponse;
        }

        public async Task<CommonResponse<PaginatedResponse<CalendarEventDto>>> GetCalendarEventsAsync(GetCalendarEventsQuery query, CancellationToken cancellationToken = default)
        {
            var filter = new CalendarEventFilter
            {
                EventType = query.EventType,
                FromAdDate = query.FromAdDate?.Date,
                ToAdDate = query.ToAdDate?.Date,
                BsYear = query.BsYear,
                IsActive = query.IsActive
            };

            var pagedEvents = await _unitOfWork.CalendarEvents.GetPagedByFilterAsync(filter, query.Page, query.PageSize, cancellationToken);

            var eventDtos = new List<CalendarEventDto>();
            foreach (var calendarEvent in pagedEvents.Items)
            {
                var eventDto = CalendarMapper.ToDto(calendarEvent);
                eventDtos.Add(eventDto);
            }

            var paginatedResponse = new PaginatedResponse<CalendarEventDto>
            {
                Items = eventDtos,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = pagedEvents.TotalCount
            };

            var successResponse = CommonResponse<PaginatedResponse<CalendarEventDto>>.Success(paginatedResponse);
            return successResponse;
        }

        public async Task<CommonResponse<CalendarEventDto>> UpdateCalendarEventAsync(Guid id, UpdateCalendarEventCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _updateEventValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<CalendarEventDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var calendarEvent = await _unitOfWork.CalendarEvents.GetByIdAsync(id, cancellationToken);
            if (calendarEvent == null)
            {
                var notFoundResponse = CommonResponse<CalendarEventDto>.Fail(ResponseCodes.NotFound, "Calendar event with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            DateTime adDate;
            int bsYear;
            int bsMonth;
            int bsDay;
            try
            {
                (adDate, bsYear, bsMonth, bsDay) = await ResolveDualDateAsync(command.IsBsDate, command.AdDate, command.BsYear, command.BsMonth, command.BsDay, cancellationToken);
            }
            catch (BsCalendarException calendarException)
            {
                var conversionFailureResponse = CommonResponse<CalendarEventDto>.Fail(ResponseCodes.ValidationError, calendarException.Message);
                return conversionFailureResponse;
            }

            calendarEvent.Title = command.Title.Trim();
            calendarEvent.EventType = command.EventType;
            calendarEvent.AdDate = adDate;
            calendarEvent.BsYear = bsYear;
            calendarEvent.BsMonth = bsMonth;
            calendarEvent.BsDay = bsDay;
            calendarEvent.Description = command.Description;
            calendarEvent.IconKey = command.IconKey;
            calendarEvent.ColorCode = command.ColorCode;
            calendarEvent.Language = string.IsNullOrWhiteSpace(command.Language) ? "en" : command.Language.Trim();
            calendarEvent.IsActive = command.IsActive;

            _unitOfWork.CalendarEvents.Update(calendarEvent);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var eventDto = CalendarMapper.ToDto(calendarEvent);
            var successResponse = CommonResponse<CalendarEventDto>.Success(eventDto, "Calendar event updated successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<bool>> DeleteCalendarEventAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var calendarEvent = await _unitOfWork.CalendarEvents.GetByIdAsync(id, cancellationToken);
            if (calendarEvent == null)
            {
                var notFoundResponse = CommonResponse<bool>.Fail(ResponseCodes.NotFound, "Calendar event with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            _unitOfWork.CalendarEvents.Remove(calendarEvent);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var successResponse = CommonResponse<bool>.Success(true, "Calendar event deleted successfully.");
            return successResponse;
        }

        // --- Festival occurrences ---

        public async Task<CommonResponse<FestivalOccurrenceDto>> CreateFestivalAsync(CreateFestivalOccurrenceCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _createFestivalValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<FestivalOccurrenceDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var festivalName = command.FestivalName.Trim();
            var duplicateExists = await _unitOfWork.CalendarEvents.FestivalExistsAsync(festivalName, command.BsYear, cancellationToken);
            if (duplicateExists)
            {
                var conflictResponse = CommonResponse<FestivalOccurrenceDto>.Fail(ResponseCodes.Conflict, "Festival '" + festivalName + "' already has an occurrence for BS year " + command.BsYear + " (possibly soft-deleted).");
                return conflictResponse;
            }

            DateTime adStartDate;
            DateTime adEndDate;
            try
            {
                adStartDate = await _conversionService.ConvertBsToAdAsync(command.BsYear, command.BsStartMonth, command.BsStartDay, cancellationToken);
                adEndDate = await _conversionService.ConvertBsToAdAsync(command.BsYear, command.BsEndMonth, command.BsEndDay, cancellationToken);
            }
            catch (BsCalendarException calendarException)
            {
                var conversionFailureResponse = CommonResponse<FestivalOccurrenceDto>.Fail(ResponseCodes.ValidationError, calendarException.Message);
                return conversionFailureResponse;
            }

            var festival = new FestivalOccurrence
            {
                FestivalName = festivalName,
                Category = command.Category,
                BsYear = command.BsYear,
                BsStartMonth = command.BsStartMonth,
                BsStartDay = command.BsStartDay,
                BsEndMonth = command.BsEndMonth,
                BsEndDay = command.BsEndDay,
                AdStartDate = adStartDate,
                AdEndDate = adEndDate,
                Description = command.Description,
                ColorCode = command.ColorCode,
                IsActive = command.IsActive
            };

            await _unitOfWork.CalendarEvents.AddFestivalAsync(festival, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var festivalDto = CalendarMapper.ToDto(festival);
            var successResponse = CommonResponse<FestivalOccurrenceDto>.Success(festivalDto, "Festival occurrence created successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<FestivalOccurrenceDto>> GetFestivalByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var festival = await _unitOfWork.CalendarEvents.GetFestivalByIdAsync(id, cancellationToken);
            if (festival == null)
            {
                var notFoundResponse = CommonResponse<FestivalOccurrenceDto>.Fail(ResponseCodes.NotFound, "Festival occurrence with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            var festivalDto = CalendarMapper.ToDto(festival);
            var successResponse = CommonResponse<FestivalOccurrenceDto>.Success(festivalDto);
            return successResponse;
        }

        public async Task<CommonResponse<List<FestivalOccurrenceDto>>> GetFestivalsAsync(int? bsYear, bool? isActive, CancellationToken cancellationToken = default)
        {
            var festivals = await _unitOfWork.CalendarEvents.GetFestivalsAsync(bsYear, isActive, cancellationToken);

            var festivalDtos = new List<FestivalOccurrenceDto>();
            foreach (var festival in festivals)
            {
                var festivalDto = CalendarMapper.ToDto(festival);
                festivalDtos.Add(festivalDto);
            }

            var successResponse = CommonResponse<List<FestivalOccurrenceDto>>.Success(festivalDtos);
            return successResponse;
        }

        public async Task<CommonResponse<FestivalOccurrenceDto>> UpdateFestivalAsync(Guid id, UpdateFestivalOccurrenceCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _updateFestivalValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<FestivalOccurrenceDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var festival = await _unitOfWork.CalendarEvents.GetFestivalByIdAsync(id, cancellationToken);
            if (festival == null)
            {
                var notFoundResponse = CommonResponse<FestivalOccurrenceDto>.Fail(ResponseCodes.NotFound, "Festival occurrence with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            var festivalName = command.FestivalName.Trim();
            var identityChanged = !string.Equals(festival.FestivalName, festivalName, StringComparison.Ordinal) || festival.BsYear != command.BsYear;
            if (identityChanged)
            {
                var duplicateExists = await _unitOfWork.CalendarEvents.FestivalExistsAsync(festivalName, command.BsYear, cancellationToken);
                if (duplicateExists)
                {
                    var conflictResponse = CommonResponse<FestivalOccurrenceDto>.Fail(ResponseCodes.Conflict, "Festival '" + festivalName + "' already has an occurrence for BS year " + command.BsYear + " (possibly soft-deleted).");
                    return conflictResponse;
                }
            }

            DateTime adStartDate;
            DateTime adEndDate;
            try
            {
                adStartDate = await _conversionService.ConvertBsToAdAsync(command.BsYear, command.BsStartMonth, command.BsStartDay, cancellationToken);
                adEndDate = await _conversionService.ConvertBsToAdAsync(command.BsYear, command.BsEndMonth, command.BsEndDay, cancellationToken);
            }
            catch (BsCalendarException calendarException)
            {
                var conversionFailureResponse = CommonResponse<FestivalOccurrenceDto>.Fail(ResponseCodes.ValidationError, calendarException.Message);
                return conversionFailureResponse;
            }

            festival.FestivalName = festivalName;
            festival.Category = command.Category;
            festival.BsYear = command.BsYear;
            festival.BsStartMonth = command.BsStartMonth;
            festival.BsStartDay = command.BsStartDay;
            festival.BsEndMonth = command.BsEndMonth;
            festival.BsEndDay = command.BsEndDay;
            festival.AdStartDate = adStartDate;
            festival.AdEndDate = adEndDate;
            festival.Description = command.Description;
            festival.ColorCode = command.ColorCode;
            festival.IsActive = command.IsActive;

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var festivalDto = CalendarMapper.ToDto(festival);
            var successResponse = CommonResponse<FestivalOccurrenceDto>.Success(festivalDto, "Festival occurrence updated successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<bool>> DeleteFestivalAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var festival = await _unitOfWork.CalendarEvents.GetFestivalByIdAsync(id, cancellationToken);
            if (festival == null)
            {
                var notFoundResponse = CommonResponse<bool>.Fail(ResponseCodes.NotFound, "Festival occurrence with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            _unitOfWork.CalendarEvents.RemoveFestival(festival);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var successResponse = CommonResponse<bool>.Success(true, "Festival occurrence deleted successfully.");
            return successResponse;
        }

        // --- Private helpers ---

        private async Task<CalendarMonthViewDto> BuildMonthViewAsync(string mode, int year, int month, DateTime startAdDate, DateTime endAdDate, CancellationToken cancellationToken)
        {
            var events = await _unitOfWork.CalendarEvents.GetActiveByAdDateRangeAsync(startAdDate, endAdDate, cancellationToken);
            var festivals = await _unitOfWork.CalendarEvents.GetActiveFestivalsByAdDateRangeAsync(startAdDate, endAdDate, cancellationToken);
            var meetings = await _unitOfWork.Meetings.GetByAdDateRangeAsync(startAdDate, endAdDate, cancellationToken);
            var weekdaySettings = await _unitOfWork.CalendarConfigs.GetWeekdayNamesAsync(cancellationToken);
            var monthNames = await _unitOfWork.CalendarConfigs.GetMonthNamesAsync(cancellationToken);
            var nepalToday = NepalDateHelper.GetNepalToday();

            var monthViewDto = new CalendarMonthViewDto
            {
                Mode = mode,
                Year = year,
                Month = month,
                TotalDays = (int)(endAdDate - startAdDate).TotalDays + 1,
                StartAdDate = startAdDate,
                EndAdDate = endAdDate
            };

            if (mode == "BS")
            {
                var bsMonthName = monthNames.FirstOrDefault(m => m.MonthNumber == month);
                monthViewDto.MonthNameEn = bsMonthName?.NameEn;
                monthViewDto.MonthNameNp = bsMonthName?.NameNp;
            }
            else
            {
                monthViewDto.MonthNameEn = new DateTime(year, month, 1).ToString("MMMM");
                monthViewDto.MonthNameNp = null;
            }

            for (var date = startAdDate; date <= endAdDate; date = date.AddDays(1))
            {
                var (bsYear, bsMonth, bsDay) = await _conversionService.ConvertAdToBsAsync(date, cancellationToken);
                var weekdayIndex = (int)date.DayOfWeek;
                var weekdaySetting = weekdaySettings.FirstOrDefault(w => w.WeekdayIndex == weekdayIndex);

                var dayDto = new CalendarDayDto
                {
                    AdDate = date,
                    AdYear = date.Year,
                    AdMonth = date.Month,
                    AdDay = date.Day,
                    BsYear = bsYear,
                    BsMonth = bsMonth,
                    BsDay = bsDay,
                    DayOfWeekIndex = weekdayIndex,
                    DayNameEn = weekdaySetting != null ? weekdaySetting.NameEn : date.DayOfWeek.ToString(),
                    DayNameNp = weekdaySetting != null ? weekdaySetting.NameNp : string.Empty,
                    IsWeeklyHoliday = weekdaySetting != null && weekdaySetting.IsWeeklyHoliday,
                    IsToday = date == nepalToday
                };

                foreach (var calendarEvent in events)
                {
                    if (calendarEvent.AdDate != date)
                    {
                        continue;
                    }

                    var eventSummary = new CalendarEventSummaryDto
                    {
                        Id = calendarEvent.Id,
                        Title = calendarEvent.Title,
                        EventType = calendarEvent.EventType,
                        ColorCode = calendarEvent.ColorCode,
                        IconKey = calendarEvent.IconKey
                    };
                    dayDto.Events.Add(eventSummary);
                }

                foreach (var festival in festivals)
                {
                    if (date < festival.AdStartDate || date > festival.AdEndDate)
                    {
                        continue;
                    }

                    var festivalSummary = new FestivalSummaryDto
                    {
                        Id = festival.Id,
                        FestivalName = festival.FestivalName,
                        Category = festival.Category,
                        ColorCode = festival.ColorCode,
                        IsStartDay = date == festival.AdStartDate,
                        IsEndDay = date == festival.AdEndDate
                    };
                    dayDto.Festivals.Add(festivalSummary);
                }

                foreach (var meeting in meetings)
                {
                    if (meeting.AdDate != date)
                    {
                        continue;
                    }

                    var meetingSummary = new MeetingSummaryDto
                    {
                        Id = meeting.Id,
                        Title = meeting.Title,
                        StartTime = meeting.StartTime,
                        EndTime = meeting.EndTime,
                        IsVirtual = meeting.IsVirtual,
                        Location = meeting.Location
                    };
                    dayDto.Meetings.Add(meetingSummary);
                }

                monthViewDto.Days.Add(dayDto);
            }

            return monthViewDto;
        }

        private async Task<DualDateDto> BuildDualDateAsync(DateTime adDate, CancellationToken cancellationToken)
        {
            var (bsYear, bsMonth, bsDay) = await _conversionService.ConvertAdToBsAsync(adDate, cancellationToken);
            var monthNames = await _unitOfWork.CalendarConfigs.GetMonthNamesAsync(cancellationToken);
            var weekdaySettings = await _unitOfWork.CalendarConfigs.GetWeekdayNamesAsync(cancellationToken);

            var monthName = monthNames.FirstOrDefault(m => m.MonthNumber == bsMonth);
            var weekdayIndex = (int)adDate.DayOfWeek;
            var weekdaySetting = weekdaySettings.FirstOrDefault(w => w.WeekdayIndex == weekdayIndex);

            var dualDateDto = new DualDateDto
            {
                AdDate = adDate,
                BsYear = bsYear,
                BsMonth = bsMonth,
                BsDay = bsDay,
                BsMonthNameEn = monthName?.NameEn,
                BsMonthNameNp = monthName?.NameNp,
                DayOfWeekIndex = weekdayIndex,
                DayNameEn = weekdaySetting != null ? weekdaySetting.NameEn : adDate.DayOfWeek.ToString(),
                DayNameNp = weekdaySetting != null ? weekdaySetting.NameNp : string.Empty
            };

            return dualDateDto;
        }

        // Resolves the canonical AD date + denormalized BS fields from whichever calendar the
        // caller entered. Throws BsCalendarException on conversion problems.
        private async Task<(DateTime AdDate, int BsYear, int BsMonth, int BsDay)> ResolveDualDateAsync(bool isBsDate, DateTime? adDateInput, int? bsYearInput, int? bsMonthInput, int? bsDayInput, CancellationToken cancellationToken)
        {
            if (isBsDate)
            {
                var adDate = await _conversionService.ConvertBsToAdAsync(bsYearInput.Value, bsMonthInput.Value, bsDayInput.Value, cancellationToken);
                return (adDate, bsYearInput.Value, bsMonthInput.Value, bsDayInput.Value);
            }

            var canonicalAdDate = adDateInput.Value.Date;
            var (bsYear, bsMonth, bsDay) = await _conversionService.ConvertAdToBsAsync(canonicalAdDate, cancellationToken);
            return (canonicalAdDate, bsYear, bsMonth, bsDay);
        }

        private static string BuildValidationErrorMessage(ValidationResult validationResult)
        {
            var errorMessages = new List<string>();
            foreach (var failure in validationResult.Errors)
            {
                errorMessages.Add(failure.ErrorMessage);
            }

            var combinedMessage = string.Join(" ", errorMessages);
            return combinedMessage;
        }
    }
}
