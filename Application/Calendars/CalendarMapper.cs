using Application.Calendars.Dtos;
using Domain.Entities;

namespace Application.Calendars
{
    public static class CalendarMapper
    {
        public static BsMonthLengthDto ToDto(BsMonthLength monthLength)
        {
            var monthLengthDto = new BsMonthLengthDto
            {
                Id = monthLength.Id,
                BsYear = monthLength.BsYear,
                BsMonth = monthLength.BsMonth,
                DaysInMonth = monthLength.DaysInMonth
            };

            return monthLengthDto;
        }

        public static BsMonthNameDto ToDto(BsMonthName monthName)
        {
            var monthNameDto = new BsMonthNameDto
            {
                Id = monthName.Id,
                MonthNumber = monthName.MonthNumber,
                NameEn = monthName.NameEn,
                NameNp = monthName.NameNp
            };

            return monthNameDto;
        }

        public static BsWeekdayNameDto ToDto(BsWeekdayName weekdayName)
        {
            var weekdayNameDto = new BsWeekdayNameDto
            {
                Id = weekdayName.Id,
                WeekdayIndex = weekdayName.WeekdayIndex,
                NameEn = weekdayName.NameEn,
                NameNp = weekdayName.NameNp,
                IsWeeklyHoliday = weekdayName.IsWeeklyHoliday
            };

            return weekdayNameDto;
        }

        public static CalendarEventDto ToDto(CalendarEvent calendarEvent)
        {
            var eventDto = new CalendarEventDto
            {
                Id = calendarEvent.Id,
                Title = calendarEvent.Title,
                EventType = calendarEvent.EventType,
                AdDate = calendarEvent.AdDate,
                BsYear = calendarEvent.BsYear,
                BsMonth = calendarEvent.BsMonth,
                BsDay = calendarEvent.BsDay,
                Description = calendarEvent.Description,
                IconKey = calendarEvent.IconKey,
                ColorCode = calendarEvent.ColorCode,
                Language = calendarEvent.Language,
                IsActive = calendarEvent.IsActive
            };

            return eventDto;
        }

        public static FestivalOccurrenceDto ToDto(FestivalOccurrence festival)
        {
            var festivalDto = new FestivalOccurrenceDto
            {
                Id = festival.Id,
                FestivalName = festival.FestivalName,
                Category = festival.Category,
                BsYear = festival.BsYear,
                BsStartMonth = festival.BsStartMonth,
                BsStartDay = festival.BsStartDay,
                BsEndMonth = festival.BsEndMonth,
                BsEndDay = festival.BsEndDay,
                AdStartDate = festival.AdStartDate,
                AdEndDate = festival.AdEndDate,
                Description = festival.Description,
                ColorCode = festival.ColorCode,
                IsActive = festival.IsActive
            };

            return festivalDto;
        }
    }
}
