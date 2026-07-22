# Dual-Calendar (Gregorian AD & Bikram Sambat BS) and Meeting Scheduling System

This guide provides a comprehensive, production-ready C# .NET 8 Web API implementation for a dual-calendar system supporting **Gregorian (AD)** and **Bikram Sambat (BS)** calendars. Unlike the Gregorian calendar, BS month lengths are not fixed and are determined dynamically by astronomical calculations published yearly by the Government of Nepal [cite: 1]. Thus, our implementation relies on database-driven configurations [cite: 1], making it highly flexible and admin-configurable [cite: 1].

In addition to calendar conversion and localized configuration, this blueprint implements a robust **Meeting Scheduling & Management** system with conflict detection and automated AD/BS synchronization.

---

## 1. System Architecture & Domain Model

The system utilizes an **AD-canonical architecture**. To ensure absolute precision, standard database date operations, and timezone consistency, all datetime data types are stored and indexed using the standard Gregorian `DateOnly` and `DateTimeOffset` formats. 
The BS equivalents (`BsYear`, `BsMonth`, `BsDay`) are denormalized and stored alongside AD fields to facilitate rapid querying, indexing, and direct rendering on localized user interfaces [cite: 4, 5].

```
┌────────────────────────────────────────────────────────────────────────┐
│                        Database-Backed Entities                        │
├──────────────────────────────────┬─────────────────────────────────────┤
│   Reference & Configuration      │          Transactional Data         │
├──────────────────────────────────┼─────────────────────────────────────┤
│  - BsMonthLength (Admin-Edit)    │  - CalendarEvent (Note/Holiday)     │
│  - BsMonthName (Localization)    │  - FestivalOccurrence (Shifting)    │
│  - BsWeekdayName (Weekly Hol.)   │  - Meeting (Host/Details)           │
│                                  │  - MeetingAttendee (Status/Rsvp)    │
└──────────────────────────────────┴─────────────────────────────────────┘
```

### 1.1 Configuration & Base Calendar Entities

#### BsMonthLength.cs
Stores the dynamic configuration of days in individual BS months [cite: 1]. This table is updated annually by administrative users to reflect the official Nepali calendar without redeploying code [cite: 1].
```csharp
using WebApi.Entities.Common;

namespace WebApi.Entities
{
    /// <summary>
    /// Number of days in one BS month of one BS year (Baisakh=1..Chaitra=12) [cite: 1]. One row per
    /// (BsYear, BsMonth) pair — the DB-backed replacement for the old hardcoded
    /// BsCalendarData.BsYearData dictionary [cite: 1]. Admin-editable so a new BS year's official
    /// month lengths (published yearly by the Nepal government) can be added without a
    /// code deployment [cite: 1].
    /// </summary>
    public class BsMonthLength : AuditableEntity
    {
        public Guid Id { get; set; }
        public int BsYear { get; set; }
        public int BsMonth { get; set; }
        public int DaysInMonth { get; set; }
    }
}
```

#### BsMonthName.cs
Provides localization for the 12 Bikram Sambat months in English and Nepali [cite: 2].
```csharp
using WebApi.Entities.Common;

namespace WebApi.Entities
{
    /// <summary>BS month display name (English/Nepali) for one of the 12 months [cite: 2]. Reference data — 12 rows [cite: 2].</summary>
    public class BsMonthName : AuditableEntity
    {
        public Guid Id { get; set; }
        public int MonthNumber { get; set; }
        public string NameEn { get; set; }
        public string NameNp { get; set; }
    }
}
```

#### BsWeekdayName.cs
Manages weekdays (0 = Sunday to 6 = Saturday) and drives regional weekend highlighting by designating standing weekly holidays (e.g., Saturdays in Nepal) [cite: 3].
```csharp
using WebApi.Entities.Common;

namespace WebApi.Entities
{
    /// <summary>Weekday display name (English/Nepali), index 0=Sunday..6=Saturday matching JS Date.getDay()/DateOnly.DayOfWeek [cite: 3]. Reference data — 7 rows [cite: 3].</summary>
    public class BsWeekdayName : AuditableEntity
    {
        public Guid Id { get; set; }
        public int WeekdayIndex { get; set; }
        public string NameEn { get; set; }
        public string NameNp { get; set; }

        /// <summary>True if this weekday is a standing weekly holiday (e.g. Saturday in Nepal) [cite: 3] —
        /// drives the frontend's weekend highlighting instead of a hardcoded day-of-week check [cite: 3].</summary>
        public bool IsWeeklyHoliday { get; set; }
    }
}
```

### 1.2 Event & Observance Entities

#### CalendarEvent.cs
Handles single-day occurrences, regional public holidays, and custom day notes [cite: 4]. Both AD and BS representations are synchronized during creation/updates [cite: 4].
```csharp
using WebApi.Common.Enums;
using WebApi.Entities.Common;

namespace WebApi.Entities
{
    /// <summary>
    /// A single-date event/observance/note (Father's Day, Constitution Day, a bilingual day
    /// description, etc.) [cite: 4] — also covers the separate "DayDescription" concept via
    /// CalendarEventType.Note [cite: 4]. AdDate is admin-entered/canonical;
    /// BsYear/Month/Day are denormalized, computed via IBsAdConversionService on save [cite: 4].
    /// </summary>
    public class CalendarEvent : AuditableEntity
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public CalendarEventType EventType { get; set; } // Note, PublicHoliday, InternalEvent
        public DateOnly AdDate { get; set; }
        public int BsYear { get; set; }
        public int BsMonth { get; set; }
        public int BsDay { get; set; }
        public string Description { get; set; }
        public string IconKey { get; set; }
        public string ColorCode { get; set; }
        public string Language { get; set; } = "en";
        public bool IsActive { get; set; } = true;
    }
}
```

#### FestivalOccurrence.cs
Accommodates lunar-based shifting festivals like Dashain or Tihar, whose BS start and end dates remain constant while their corresponding Gregorian AD dates drift significantly year-over-year [cite: 5].
```csharp
using WebApi.Common.Enums;
using WebApi.Entities.Common;

namespace WebApi.Entities
{
    /// <summary>
    /// A festival's dates for one specific BS year (Dashain, Tihar, etc. shift every year, unlike
    /// a fixed Holiday) [cite: 5]. AdStartDate/AdEndDate are denormalized — computed once via
    /// IBsAdConversionService when the row is created/updated, purely so date-range queries don't
    /// need to re-run the conversion loop per request [cite: 5]. The BS start/end fields remain the source
    /// of truth an admin edits [cite: 5].
    /// </summary>
    public class FestivalOccurrence : AuditableEntity
    {
        public Guid Id { get; set; }
        public string FestivalName { get; set; }
        public HolidayType Category { get; set; } // National, Religious, Regional
        public int BsYear { get; set; }
        public int BsStartMonth { get; set; }
        public int BsStartDay { get; set; }
        public int BsEndMonth { get; set; }
        public int BsEndDay { get; set; }
        public DateOnly AdStartDate { get; set; }
        public DateOnly AdEndDate { get; set; }
        public string Description { get; set; }
        public string ColorCode { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
```

### 1.3 Meeting Scheduler Entities

To satisfy meeting schedule requirements, we introduce the transactional `Meeting` and relational `MeetingAttendee` entities, complete with calendar status tracking.

#### Meeting.cs
```csharp
using WebApi.Entities.Common;

namespace WebApi.Entities
{
    public class Meeting : AuditableEntity
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        
        // Canonical Datetime Fields (UTC Storage Preferred)
        public DateOnly AdDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        
        // Synchronized BS Denormalized Date Fields
        public int BsYear { get; set; }
        public int BsMonth { get; set; }
        public int BsDay { get; set; }
        
        // Location Details
        public bool IsVirtual { get; set; }
        public string Location { get; set; } // Physical room name or virtual URL (Zoom/Teams)
        
        // Relations
        public Guid HostUserId { get; set; }
        public virtual ICollection<MeetingAttendee> Attendees { get; set; } = new List<MeetingAttendee>();
    }
}
```

#### MeetingAttendee.cs
```csharp
using WebApi.Entities.Common;

namespace WebApi.Entities
{
    public class MeetingAttendee : AuditableEntity
    {
        public Guid Id { get; set; }
        public Guid MeetingId { get; set; }
        public virtual Meeting Meeting { get; set; }
        
        public Guid UserId { get; set; }
        public string Email { get; set; }
        public AttendeeStatus Status { get; set; } = AttendeeStatus.Pending; // Pending, Accepted, Declined, Tentative
    }
    
    public enum AttendeeStatus
    {
        Pending,
        Accepted,
        Declined,
        Tentative
    }
}
```

---

## 2. Core Conversion Engine (`IBsAdConversionService`)

Because BS calendar month-lengths fluctuate dynamically based on astrological charts [cite: 1], bidirectional date conversions are performed through a **cumulative day-offset algorithm**. 

The engine uses a configurable **base anchor point**:
* **Anchor Date BS**: `2000-01-01` (Baisakh 1, 2000)
* **Anchor Date AD**: `1943-04-13` (April 13, 1943)

Conversions parse total elapsed days between the source date and anchor point using cached DB records of `BsMonthLength` [cite: 1].

### 2.1 Service Contract

```csharp
namespace WebApi.Services
{
    public interface IBsAdConversionService
    {
        Task<DateOnly> ConvertBsToAdAsync(int bsYear, int bsMonth, int bsDay);
        Task<(int BsYear, int BsMonth, int BsDay)> ConvertAdToBsAsync(DateOnly adDate);
        Task<int> GetDaysInBsMonthAsync(int bsYear, int bsMonth);
    }
}
```

### 2.2 Implementation Details

This service accesses DB-stored calendar maps to handle the math safely.

```csharp
using Microsoft.EntityFrameworkCore;
using WebApi.Entities;
using WebApi.Persistence;

namespace WebApi.Services
{
    public class BsAdConversionService : IBsAdConversionService
    {
        private readonly ApplicationDbContext _context;
        
        // Anchor Constants
        private static readonly DateOnly AnchorAdDate = new(1943, 4, 13);
        private const int AnchorBsYear = 2000;
        private const int AnchorBsMonth = 1;
        private const int AnchorBsDay = 1;

        public BsAdConversionService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int> GetDaysInBsMonthAsync(int bsYear, int bsMonth)
        {
            var length = await _context.BsMonthLengths
                .FirstOrDefaultAsync(x => x.BsYear == bsYear && x.BsMonth == bsMonth);

            if (length == null)
            {
                throw new InvalidOperationException(
                    $"BS Month configuration missing for Year: {bsYear}, Month: {bsMonth}. Please update calendar configuration."
                );
            }
            return length.DaysInMonth;
        }

        public async Task<DateOnly> ConvertBsToAdAsync(int bsYear, int bsMonth, int bsDay)
        {
            if (bsYear < AnchorBsYear)
                throw new ArgumentOutOfRangeException(nameof(bsYear), "Conversions prior to BS 2000 are not supported.");

            long totalDaysOffset = 0;

            // 1. Calculate days for elapsed years since the anchor
            var monthLengths = await _context.BsMonthLengths
                .Where(x => x.BsYear >= AnchorBsYear && x.BsYear <= bsYear)
                .OrderBy(x => x.BsYear)
                .ThenBy(x => x.BsMonth)
                .ToListAsync();

            // Sum days for fully completed years
            for (int y = AnchorBsYear; y < bsYear; y++)
            {
                var yearMonths = monthLengths.Where(x => x.BsYear == y);
                if (yearMonths.Count() < 12)
                    throw new InvalidOperationException($"Incomplete month lengths configured for BS year {y}.");
                totalDaysOffset += yearMonths.Sum(x => x.DaysInMonth);
            }

            // Sum days for completed months in the target year
            for (int m = 1; m < bsMonth; m++)
            {
                var targetMonth = monthLengths.FirstOrDefault(x => x.BsYear == bsYear && x.BsMonth == m);
                if (targetMonth == null)
                    throw new InvalidOperationException($"Month length configuration missing for BS year {bsYear}, month {m}.");
                totalDaysOffset += targetMonth.DaysInMonth;
            }

            // Add offset days in target month (1-based, so subtract anchor day)
            totalDaysOffset += (bsDay - AnchorBsDay);

            return AnchorAdDate.AddDays((int)totalDaysOffset);
        }

        public async Task<(int BsYear, int BsMonth, int BsDay)> ConvertAdToBsAsync(DateOnly adDate)
        {
            if (adDate < AnchorAdDate)
                throw new ArgumentOutOfRangeException(nameof(adDate), "Conversions prior to AD 1943-04-13 are not supported.");

            int totalDaysOffset = adDate.DayNumber - AnchorAdDate.DayNumber;

            int currentBsYear = AnchorBsYear;
            int currentBsMonth = AnchorBsMonth;

            // Query configured lengths in batches starting from target boundary
            var allConfigs = await _context.BsMonthLengths
                .Where(x => x.BsYear >= AnchorBsYear)
                .OrderBy(x => x.BsYear)
                .ThenBy(x => x.BsMonth)
                .ToListAsync();

            int configIdx = 0;

            while (totalDaysOffset > 0)
            {
                if (configIdx >= allConfigs.Count)
                {
                    throw new InvalidOperationException(
                        $"Exceeded configured calendar mappings. Please add configuration data for BS Year {currentBsYear}."
                    );
                }

                var currentMonthConfig = allConfigs[configIdx];
                int daysInCurrentMonth = currentMonthConfig.DaysInMonth;

                if (totalDaysOffset >= daysInCurrentMonth)
                {
                    totalDaysOffset -= daysInCurrentMonth;
                    
                    // Increment Month/Year tracking
                    if (currentBsMonth == 12)
                    {
                        currentBsMonth = 1;
                        currentBsYear++;
                    }
                    else
                    {
                        currentBsMonth++;
                    }
                    configIdx++;
                }
                else
                {
                    break;
                }
            }

            int currentBsDay = AnchorBsDay + totalDaysOffset;
            return (currentBsYear, currentBsMonth, currentBsDay);
        }
    }
}
```

---

## 3. Entity Framework Core Persistence Configuration

The Database Context links conversion dependencies with entity events, computing both standard AD and BS mappings immediately during entity mutations [cite: 4, 5].

```csharp
using Microsoft.EntityFrameworkCore;
using WebApi.Entities;

namespace WebApi.Persistence
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<BsMonthLength> BsMonthLengths { get; set; }
        public DbSet<BsMonthName> BsMonthNames { get; set; }
        public DbSet<BsWeekdayName> BsWeekdayNames { get; set; }
        public DbSet<CalendarEvent> CalendarEvents { get; set; }
        public DbSet<FestivalOccurrence> FestivalOccurrences { get; set; }
        public DbSet<Meeting> Meetings { get; set; }
        public DbSet<MeetingAttendee> MeetingAttendees { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Indexing & Keys
            modelBuilder.Entity<BsMonthLength>(entity =>
            {
                entity.HasIndex(e => new { e.BsYear, e.BsMonth }).IsUnique();
                entity.Property(e => e.DaysInMonth).IsRequired();
            });

            modelBuilder.Entity<BsMonthName>(entity =>
            {
                entity.HasIndex(e => e.MonthNumber).IsUnique();
                entity.Property(e => e.NameEn).IsRequired().HasMaxLength(50);
                entity.Property(e => e.NameNp).IsRequired().HasMaxLength(50);
            });

            modelBuilder.Entity<BsWeekdayName>(entity =>
            {
                entity.HasIndex(e => e.WeekdayIndex).IsUnique();
                entity.Property(e => e.NameEn).IsRequired().HasMaxLength(50);
                entity.Property(e => e.NameNp).IsRequired().HasMaxLength(50);
            });

            modelBuilder.Entity<CalendarEvent>(entity =>
            {
                entity.HasIndex(e => e.AdDate);
                entity.HasIndex(e => new { e.BsYear, e.BsMonth, e.BsDay });
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            });

            modelBuilder.Entity<FestivalOccurrence>(entity =>
            {
                entity.HasIndex(e => new { e.AdStartDate, e.AdEndDate });
                entity.HasIndex(e => e.BsYear);
                entity.Property(e => e.FestivalName).IsRequired().HasMaxLength(200);
            });

            modelBuilder.Entity<Meeting>(entity =>
            {
                entity.HasIndex(e => e.AdDate);
                entity.HasIndex(e => new { e.BsYear, e.BsMonth, e.BsDay });
                entity.Property(e => e.Title).IsRequired().HasMaxLength(250);
            });

            modelBuilder.Entity<MeetingAttendee>(entity =>
            {
                entity.HasIndex(e => new { e.MeetingId, e.UserId }).IsUnique();
                entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
            });
        }
    }
}
```

---

## 4. Web API Controllers & Service Interfacing

### 4.1 Month Configuration & Localization Endpoint
Administers database month mappings [cite: 1] and localized display names [cite: 2, 3].

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApi.Entities;
using WebApi.Persistence;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/calendar-configuration")]
    public class CalendarConfigController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CalendarConfigController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Configure dynamic BS month length [cite: 1]
        [HttpPost("bs-month-length")]
        public async Task<IActionResult> ConfigureBsMonthLength([FromBody] List<BsMonthLengthDto> payload)
        {
            foreach (var item in payload)
            {
                var existing = await _context.BsMonthLengths
                    .FirstOrDefaultAsync(x => x.BsYear == item.BsYear && x.BsMonth == item.BsMonth);

                if (existing != null)
                {
                    existing.DaysInMonth = item.DaysInMonth;
                }
                else
                {
                    _context.BsMonthLengths.Add(new BsMonthLength
                    {
                        BsYear = item.BsYear,
                        BsMonth = item.BsMonth,
                        DaysInMonth = item.DaysInMonth
                    });
                }
            }
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Bikram Sambat Month configuration successfully synchronized." });
        }

        // Fetch display names for BS UI Calendar [cite: 2, 3]
        [HttpGet("localization-data")]
        public async Task<IActionResult> GetLocalizationData()
        {
            var months = await _context.BsMonthNames.OrderBy(x => x.MonthNumber).ToListAsync();
            var weekdays = await _context.BsWeekdayNames.OrderBy(x => x.WeekdayIndex).ToListAsync();
            return Ok(new { Months = months, Weekdays = weekdays });
        }
    }

    public class BsMonthLengthDto
    {
        public int BsYear { get; set; }
        public int BsMonth { get; set; }
        public int DaysInMonth { get; set; }
    }
}
```

### 4.2 Unified Calendar View Controller
Provides a structured monthly matrix detailing standard dates, holidays, custom events [cite: 4], and meetings scheduled within the selected timeframe.

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApi.Entities;
using WebApi.Persistence;
using WebApi.Services;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/calendar")]
    public class CalendarController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IBsAdConversionService _converter;

        public CalendarController(ApplicationDbContext context, IBsAdConversionService converter)
        {
            _context = context;
            _converter = converter;
        }

        [HttpGet("month-view")]
        public async Task<IActionResult> GetMonthView([FromQuery] int year, [FromQuery] int month, [FromQuery] string mode = "BS")
        {
            DateOnly startDateAd;
            DateOnly endDateAd;

            if (mode.ToUpper() == "BS")
            {
                int totalDays = await _converter.GetDaysInBsMonthAsync(year, month);
                startDateAd = await _converter.ConvertBsToAdAsync(year, month, 1);
                endDateAd = await _converter.ConvertBsToAdAsync(year, month, totalDays);
            }
            else // AD Mode
            {
                startDateAd = new DateOnly(year, month, 1);
                endDateAd = new DateOnly(year, month, DateTime.DaysInMonth(year, month));
            }

            // Retrieve events, festivals, and meetings matching the dates
            var events = await _context.CalendarEvents
                .Where(e => e.AdDate >= startDateAd && e.AdDate <= endDateAd && e.IsActive)
                .ToListAsync();

            var festivals = await _context.FestivalOccurrences
                .Where(f => f.AdStartDate <= endDateAd && f.AdEndDate >= startDateAd && f.IsActive)
                .ToListAsync();

            var meetings = await _context.Meetings
                .Include(m => m.Attendees)
                .Where(m => m.AdDate >= startDateAd && m.AdDate <= endDateAd)
                .ToListAsync();

            var weekdaySettings = await _context.BsWeekdayNames.ToListAsync();

            // Construct Response Grid
            var daysList = new List<CalendarDayDto>();
            for (var d = startDateAd; d <= endDateAd; d = d.AddDays(1))
            {
                var (bsY, bsM, bsD) = await _converter.ConvertAdToBsAsync(d);
                var weekdayIndex = (int)d.DayOfWeek;
                var weekdaySetting = weekdaySettings.FirstOrDefault(w => w.WeekdayIndex == weekdayIndex);

                daysList.Add(new CalendarDayDto
                {
                    AdDate = d,
                    BsYear = bsY,
                    BsMonth = bsM,
                    BsDay = bsD,
                    DayOfWeekIndex = weekdayIndex,
                    DayNameEn = weekdaySetting?.NameEn ?? d.DayOfWeek.ToString(),
                    DayNameNp = weekdaySetting?.NameNp ?? "",
                    IsWeeklyHoliday = weekdaySetting?.IsWeeklyHoliday ?? false,
                    Events = events.Where(e => e.AdDate == d).Select(e => new CalendarEventSummaryDto { Title = e.Title, ColorCode = e.ColorCode, EventType = e.EventType.ToString() }).ToList(),
                    Festivals = festivals.Where(f => d >= f.AdStartDate && d <= f.AdEndDate).Select(f => f.FestivalName).ToList(),
                    Meetings = meetings.Where(m => m.AdDate == d).Select(m => new MeetingSummaryDto { MeetingId = m.Id, Title = m.Title, StartTime = m.StartTime, EndTime = m.EndTime, IsVirtual = m.IsVirtual }).ToList()
                });
            }

            return Ok(new { RequestedMode = mode, RequestedYear = year, RequestedMonth = month, Days = daysList });
        }
    }

    public class CalendarDayDto
    {
        public DateOnly AdDate { get; set; }
        public int BsYear { get; set; }
        public int BsMonth { get; set; }
        public int BsDay { get; set; }
        public int DayOfWeekIndex { get; set; }
        public string DayNameEn { get; set; }
        public string DayNameNp { get; set; }
        public bool IsWeeklyHoliday { get; set; }
        public List<CalendarEventSummaryDto> Events { get; set; } = new();
        public List<string> Festivals { get; set; } = new();
        public List<MeetingSummaryDto> Meetings { get; set; } = new();
    }

    public class CalendarEventSummaryDto
    {
        public string Title { get; set; }
        public string ColorCode { get; set; }
        public string EventType { get; set; }
    }

    public class MeetingSummaryDto
    {
        public Guid MeetingId { get; set; }
        public string Title { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public bool IsVirtual { get; set; }
    }
}
```

### 4.3 Interactive Meeting Scheduler Controller
Features conflict checks, invites participants, and writes back mapped calendar positions immediately on submission.

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApi.Entities;
using WebApi.Persistence;
using WebApi.Services;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/meetings")]
    public class MeetingController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IBsAdConversionService _converter;

        public MeetingController(ApplicationDbContext context, IBsAdConversionService converter)
        {
            _context = context;
            _converter = converter;
        }

        [HttpPost("schedule")]
        public async Task<IActionResult> ScheduleMeeting([FromBody] CreateMeetingDto dto)
        {
            DateOnly adTargetDate;
            int bsYear, bsMonth, bsDay;

            // 1. Resolve localized calendar dates
            if (dto.IsBsScheduled)
            {
                bsYear = dto.ScheduledYear;
                bsMonth = dto.ScheduledMonth;
                bsDay = dto.ScheduledDay;
                adTargetDate = await _converter.ConvertBsToAdAsync(bsYear, bsMonth, bsDay);
            }
            else
            {
                adTargetDate = DateOnly.FromDateTime(dto.ScheduledAdDate);
                var bsDate = await _converter.ConvertAdToBsAsync(adTargetDate);
                bsYear = bsDate.BsYear;
                bsMonth = bsDate.BsMonth;
                bsDay = bsDate.BsDay;
            }

            // 2. Room/Scheduler Conflict Detection Block
            var conflictsExist = await _context.Meetings.AnyAsync(m =>
                m.AdDate == adTargetDate &&
                m.HostUserId == dto.HostUserId &&
                ((dto.StartTime >= m.StartTime && dto.StartTime < m.EndTime) ||
                 (dto.EndTime > m.StartTime && dto.EndTime <= m.EndTime) ||
                 (dto.StartTime <= m.StartTime && dto.EndTime >= m.EndTime))
            );

            if (conflictsExist)
            {
                return BadRequest(new { Error = "Scheduling Conflict. The organizer is already booked for this time block." });
            }

            // 3. Construct meeting transaction record
            var meeting = new Meeting
            {
                Title = dto.Title,
                Description = dto.Description,
                AdDate = adTargetDate,
                BsYear = bsYear,
                BsMonth = bsMonth,
                BsDay = bsDay,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                IsVirtual = dto.IsVirtual,
                Location = dto.Location,
                HostUserId = dto.HostUserId
            };

            foreach (var email in dto.AttendeeEmails)
            {
                meeting.Attendees.Add(new MeetingAttendee
                {
                    Email = email,
                    Status = AttendeeStatus.Pending
                });
            }

            _context.Meetings.Add(meeting);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMeetingById), new { id = meeting.Id }, meeting);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetMeetingById(Guid id)
        {
            var meeting = await _context.Meetings
                .Include(m => m.Attendees)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (meeting == null) return NotFound();
            return Ok(meeting);
        }

        [HttpPost("respond")]
        public async Task<IActionResult> RespondToInvitation([FromBody] RespondInvitationDto dto)
        {
            var attendee = await _context.MeetingAttendees
                .FirstOrDefaultAsync(a => a.MeetingId == dto.MeetingId && a.Email == dto.Email);

            if (attendee == null)
            {
                return NotFound(new { Error = "Attendee details not located on current meeting register." });
            }

            attendee.Status = dto.Status;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Attendee status successfully registered." });
        }
    }

    public class CreateMeetingDto
    {
        public string Title { get; set; }
        public string Description { get; set; }
        
        // Parameter sets to determine AD vs BS Scheduling Strategy
        public bool IsBsScheduled { get; set; }
        public DateTime ScheduledAdDate { get; set; }
        public int ScheduledYear { get; set; }
        public int ScheduledMonth { get; set; }
        public int ScheduledDay { get; set; }

        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public bool IsVirtual { get; set; }
        public string Location { get; set; }
        public Guid HostUserId { get; set; }
        public List<string> AttendeeEmails { get; set; } = new();
    }

    public class RespondInvitationDto
    {
        public Guid MeetingId { get; set; }
        public string Email { get; set; }
        public AttendeeStatus Status { get; set; }
    }
}
```

---

## 5. Front-End Payload & UI Architecture Guidance

When mapping the output of `GET /api/calendar/month-view` to a standard tabular grid UI layout, the following rules apply:

1. **Flexible Grid Columns**: In standard AD display modes, use standard Gregorian offsets (Sunday-Saturday). In BS mode, rely on dynamic month lengths [cite: 1] starting from the mapped weekday index of Baisakh 1.
2. **Holiday Styling Overlay**: Highlight columns where `IsWeeklyHoliday` is returned true [cite: 3]. Render standard color tags matching background rules for active events [cite: 4] and festivals [cite: 5].
3. **Meetings Rendering**: Append interactive schedules under dates displaying matching list items with click events linking back to participant RSVP dialogues.
