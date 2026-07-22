using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Persistence.DataSeeder
{
    // Seeds the BS calendar reference data the dual-calendar feature runs on:
    //   - bs_month_names   (12 rows, English + Nepali)
    //   - bs_weekday_names (7 rows, Saturday flagged as the weekly holiday)
    //   - bs_month_lengths (BS 2000-2090, the standard published table -- verified against
    //     known checkpoints: Baisakh 1 of BS 2072/2077/2081/2082/2083 landing on their known
    //     AD dates, and Constitution Day 2072-06-03 == 2015-09-20, with the
    //     BS 2000-01-01 == AD 1943-04-14 anchor BsAdConversionService uses)
    //
    // Idempotent and strictly create-if-missing per table (month/weekday names) or per
    // (year, month) row (month lengths), so admin edits -- a corrected month length, a
    // renamed weekday, a changed weekly-holiday flag -- survive restarts, same convention as
    // AppConfigSeeder. Later BS years (2091+) are added at runtime via
    // POST /api/calendar-configuration/bs-month-lengths when the government publishes them.
    public static class CalendarSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            await SeedMonthNamesAsync(dbContext);
            await SeedWeekdayNamesAsync(dbContext);
            await SeedMonthLengthsAsync(dbContext);
        }

        private static async Task SeedMonthNamesAsync(ApplicationDbContext dbContext)
        {
            var monthNamesExist = await dbContext.Set<BsMonthName>().AnyAsync();
            if (monthNamesExist)
            {
                return;
            }

            var monthNames = new List<BsMonthName>
            {
                NewMonthName(1, "Baisakh", "वैशाख"),
                NewMonthName(2, "Jestha", "जेठ"),
                NewMonthName(3, "Ashadh", "असार"),
                NewMonthName(4, "Shrawan", "साउन"),
                NewMonthName(5, "Bhadra", "भदौ"),
                NewMonthName(6, "Ashwin", "असोज"),
                NewMonthName(7, "Kartik", "कार्तिक"),
                NewMonthName(8, "Mangsir", "मंसिर"),
                NewMonthName(9, "Poush", "पुष"),
                NewMonthName(10, "Magh", "माघ"),
                NewMonthName(11, "Falgun", "फागुन"),
                NewMonthName(12, "Chaitra", "चैत")
            };

            dbContext.Set<BsMonthName>().AddRange(monthNames);
            await dbContext.SaveChangesAsync();
        }

        private static async Task SeedWeekdayNamesAsync(ApplicationDbContext dbContext)
        {
            var weekdayNamesExist = await dbContext.Set<BsWeekdayName>().AnyAsync();
            if (weekdayNamesExist)
            {
                return;
            }

            var weekdayNames = new List<BsWeekdayName>
            {
                NewWeekdayName(0, "Sunday", "आइतबार", false),
                NewWeekdayName(1, "Monday", "सोमबार", false),
                NewWeekdayName(2, "Tuesday", "मंगलबार", false),
                NewWeekdayName(3, "Wednesday", "बुधबार", false),
                NewWeekdayName(4, "Thursday", "बिहीबार", false),
                NewWeekdayName(5, "Friday", "शुक्रबार", false),
                NewWeekdayName(6, "Saturday", "शनिबार", true)
            };

            dbContext.Set<BsWeekdayName>().AddRange(weekdayNames);
            await dbContext.SaveChangesAsync();
        }

        private static async Task SeedMonthLengthsAsync(ApplicationDbContext dbContext)
        {
            var existingRows = await dbContext.Set<BsMonthLength>()
                .Select(m => new { m.BsYear, m.BsMonth })
                .ToListAsync();

            var existingPairs = new HashSet<(int BsYear, int BsMonth)>();
            foreach (var row in existingRows)
            {
                existingPairs.Add((row.BsYear, row.BsMonth));
            }

            var newRows = new List<BsMonthLength>();
            foreach (var yearEntry in BsYearData)
            {
                for (var month = 1; month <= 12; month++)
                {
                    if (existingPairs.Contains((yearEntry.Key, month)))
                    {
                        continue;
                    }

                    var monthLength = new BsMonthLength
                    {
                        BsYear = yearEntry.Key,
                        BsMonth = month,
                        DaysInMonth = yearEntry.Value[month - 1]
                    };

                    newRows.Add(monthLength);
                }
            }

            if (newRows.Count == 0)
            {
                return;
            }

            dbContext.Set<BsMonthLength>().AddRange(newRows);
            await dbContext.SaveChangesAsync();
        }

        private static BsMonthName NewMonthName(int monthNumber, string nameEn, string nameNp)
        {
            var monthName = new BsMonthName
            {
                MonthNumber = monthNumber,
                NameEn = nameEn,
                NameNp = nameNp
            };

            return monthName;
        }

        private static BsWeekdayName NewWeekdayName(int weekdayIndex, string nameEn, string nameNp, bool isWeeklyHoliday)
        {
            var weekdayName = new BsWeekdayName
            {
                WeekdayIndex = weekdayIndex,
                NameEn = nameEn,
                NameNp = nameNp,
                IsWeeklyHoliday = isWeeklyHoliday
            };

            return weekdayName;
        }

        // The standard published BS month-length table, one entry per BS year (index 0 =
        // Baisakh .. index 11 = Chaitra). Kept in code (not config) because it is structural
        // reference data, same reasoning as MenuSeeder's catalog.
        private static readonly Dictionary<int, int[]> BsYearData = new Dictionary<int, int[]>
        {
            { 2000, new[] { 30, 32, 31, 32, 31, 30, 30, 30, 29, 30, 29, 31 } },
            { 2001, new[] { 31, 31, 32, 31, 31, 31, 30, 29, 30, 29, 30, 30 } },
            { 2002, new[] { 31, 31, 32, 32, 31, 30, 30, 29, 30, 29, 30, 30 } },
            { 2003, new[] { 31, 32, 31, 32, 31, 30, 30, 30, 29, 29, 30, 31 } },
            { 2004, new[] { 30, 32, 31, 32, 31, 30, 30, 30, 29, 30, 29, 31 } },
            { 2005, new[] { 31, 31, 32, 31, 31, 31, 30, 29, 30, 29, 30, 30 } },
            { 2006, new[] { 31, 31, 32, 32, 31, 30, 30, 29, 30, 29, 30, 30 } },
            { 2007, new[] { 31, 32, 31, 32, 31, 30, 30, 30, 29, 29, 30, 31 } },
            { 2008, new[] { 31, 31, 31, 32, 31, 31, 29, 30, 30, 29, 29, 31 } },
            { 2009, new[] { 31, 31, 32, 31, 31, 31, 30, 29, 30, 29, 30, 30 } },
            { 2010, new[] { 31, 31, 32, 32, 31, 30, 30, 29, 30, 29, 30, 30 } },
            { 2011, new[] { 31, 32, 31, 32, 31, 30, 30, 30, 29, 29, 30, 31 } },
            { 2012, new[] { 31, 31, 31, 32, 31, 31, 29, 30, 30, 29, 30, 30 } },
            { 2013, new[] { 31, 31, 32, 31, 31, 31, 30, 29, 30, 29, 30, 30 } },
            { 2014, new[] { 31, 31, 32, 32, 31, 30, 30, 29, 30, 29, 30, 30 } },
            { 2015, new[] { 31, 32, 31, 32, 31, 30, 30, 30, 29, 29, 30, 31 } },
            { 2016, new[] { 31, 31, 31, 32, 31, 31, 29, 30, 30, 29, 30, 30 } },
            { 2017, new[] { 31, 31, 32, 31, 31, 31, 30, 29, 30, 29, 30, 30 } },
            { 2018, new[] { 31, 32, 31, 32, 31, 30, 30, 29, 30, 29, 30, 30 } },
            { 2019, new[] { 31, 32, 31, 32, 31, 30, 30, 30, 29, 30, 29, 31 } },
            { 2020, new[] { 31, 31, 31, 32, 31, 31, 30, 29, 30, 29, 30, 30 } },
            { 2021, new[] { 31, 31, 32, 31, 31, 31, 30, 29, 30, 29, 30, 30 } },
            { 2022, new[] { 31, 32, 31, 32, 31, 30, 30, 30, 29, 29, 30, 30 } },
            { 2023, new[] { 31, 32, 31, 32, 31, 30, 30, 30, 29, 30, 29, 31 } },
            { 2024, new[] { 31, 31, 31, 32, 31, 31, 30, 29, 30, 29, 30, 30 } },
            { 2025, new[] { 31, 31, 32, 31, 31, 31, 30, 29, 30, 29, 30, 30 } },
            { 2026, new[] { 31, 32, 31, 32, 31, 30, 30, 30, 29, 29, 30, 31 } },
            { 2027, new[] { 30, 32, 31, 32, 31, 30, 30, 30, 29, 30, 29, 31 } },
            { 2028, new[] { 31, 31, 32, 31, 31, 31, 30, 29, 30, 29, 30, 30 } },
            { 2029, new[] { 31, 31, 32, 31, 32, 30, 30, 29, 30, 29, 30, 30 } },
            { 2030, new[] { 31, 32, 31, 32, 31, 30, 30, 30, 29, 30, 29, 31 } },
            { 2031, new[] { 30, 32, 31, 32, 31, 30, 30, 30, 29, 30, 29, 31 } },
            { 2032, new[] { 31, 31, 32, 31, 31, 31, 30, 29, 30, 29, 30, 30 } },
            { 2033, new[] { 31, 31, 32, 32, 31, 30, 30, 29, 30, 29, 30, 30 } },
            { 2034, new[] { 31, 32, 31, 32, 31, 30, 30, 30, 29, 29, 30, 31 } },
            { 2035, new[] { 30, 32, 31, 32, 31, 31, 29, 30, 30, 29, 29, 31 } },
            { 2036, new[] { 31, 31, 32, 31, 31, 31, 30, 29, 30, 29, 30, 30 } },
            { 2037, new[] { 31, 31, 32, 32, 31, 30, 30, 29, 30, 29, 30, 30 } },
            { 2038, new[] { 31, 32, 31, 32, 31, 30, 30, 30, 29, 29, 30, 31 } },
            { 2039, new[] { 31, 31, 31, 32, 31, 31, 29, 30, 30, 29, 30, 30 } },
            { 2040, new[] { 31, 31, 32, 31, 31, 31, 30, 29, 30, 29, 30, 30 } },
            { 2041, new[] { 31, 31, 32, 32, 31, 30, 30, 29, 30, 29, 30, 30 } },
            { 2042, new[] { 31, 32, 31, 32, 31, 30, 30, 30, 29, 29, 30, 31 } },
            { 2043, new[] { 31, 31, 31, 32, 31, 31, 29, 30, 30, 29, 30, 30 } },
            { 2044, new[] { 31, 31, 32, 31, 31, 31, 30, 29, 30, 29, 30, 30 } },
            { 2045, new[] { 31, 32, 31, 32, 31, 30, 30, 29, 30, 29, 30, 30 } },
            { 2046, new[] { 31, 32, 31, 32, 31, 30, 30, 30, 29, 29, 30, 31 } },
            { 2047, new[] { 31, 31, 31, 32, 31, 31, 30, 29, 30, 29, 30, 30 } },
            { 2048, new[] { 31, 31, 32, 31, 31, 31, 30, 29, 30, 29, 30, 30 } },
            { 2049, new[] { 31, 32, 31, 32, 31, 30, 30, 30, 29, 29, 30, 30 } },
            { 2050, new[] { 31, 32, 31, 32, 31, 30, 30, 30, 29, 30, 29, 31 } },
            { 2051, new[] { 31, 31, 31, 32, 31, 31, 30, 29, 30, 29, 30, 30 } },
            { 2052, new[] { 31, 31, 32, 31, 31, 31, 30, 29, 30, 29, 30, 30 } },
            { 2053, new[] { 31, 32, 31, 32, 31, 30, 30, 30, 29, 29, 30, 30 } },
            { 2054, new[] { 31, 32, 31, 32, 31, 30, 30, 30, 29, 30, 29, 31 } },
            { 2055, new[] { 31, 31, 32, 31, 31, 31, 30, 29, 30, 29, 30, 30 } },
            { 2056, new[] { 31, 31, 32, 31, 32, 30, 30, 29, 30, 29, 30, 30 } },
            { 2057, new[] { 31, 32, 31, 32, 31, 30, 30, 30, 29, 30, 29, 31 } },
            { 2058, new[] { 30, 32, 31, 32, 31, 30, 30, 30, 29, 30, 29, 31 } },
            { 2059, new[] { 31, 31, 32, 31, 31, 31, 30, 29, 30, 29, 30, 30 } },
            { 2060, new[] { 31, 31, 32, 32, 31, 30, 30, 29, 30, 29, 30, 30 } },
            { 2061, new[] { 31, 32, 31, 32, 31, 30, 30, 30, 29, 29, 30, 31 } },
            { 2062, new[] { 30, 32, 31, 32, 31, 31, 29, 30, 29, 30, 29, 31 } },
            { 2063, new[] { 31, 31, 32, 31, 31, 31, 30, 29, 30, 29, 30, 30 } },
            { 2064, new[] { 31, 31, 32, 32, 31, 30, 30, 29, 30, 29, 30, 30 } },
            { 2065, new[] { 31, 32, 31, 32, 31, 30, 30, 30, 29, 29, 30, 31 } },
            { 2066, new[] { 31, 31, 31, 32, 31, 31, 29, 30, 30, 29, 29, 31 } },
            { 2067, new[] { 31, 31, 32, 31, 31, 31, 30, 29, 30, 29, 30, 30 } },
            { 2068, new[] { 31, 31, 32, 32, 31, 30, 30, 29, 30, 29, 30, 30 } },
            { 2069, new[] { 31, 32, 31, 32, 31, 30, 30, 30, 29, 29, 30, 31 } },
            { 2070, new[] { 31, 31, 31, 32, 31, 31, 29, 30, 30, 29, 30, 30 } },
            { 2071, new[] { 31, 31, 32, 31, 31, 31, 30, 29, 30, 29, 30, 30 } },
            { 2072, new[] { 31, 32, 31, 32, 31, 30, 30, 29, 30, 29, 30, 30 } },
            { 2073, new[] { 31, 32, 31, 32, 31, 30, 30, 30, 29, 29, 30, 31 } },
            { 2074, new[] { 31, 31, 31, 32, 31, 31, 30, 29, 30, 29, 30, 30 } },
            { 2075, new[] { 31, 31, 32, 31, 31, 31, 30, 29, 30, 29, 30, 30 } },
            { 2076, new[] { 31, 32, 31, 32, 31, 30, 30, 30, 29, 29, 30, 30 } },
            { 2077, new[] { 31, 32, 31, 32, 31, 30, 30, 30, 29, 30, 29, 31 } },
            { 2078, new[] { 31, 31, 31, 32, 31, 31, 30, 29, 30, 29, 30, 30 } },
            { 2079, new[] { 31, 31, 32, 31, 31, 31, 30, 29, 30, 29, 30, 30 } },
            { 2080, new[] { 31, 32, 31, 32, 31, 30, 30, 30, 29, 29, 30, 30 } },
            { 2081, new[] { 31, 31, 32, 32, 31, 30, 30, 30, 29, 30, 30, 30 } },
            { 2082, new[] { 30, 32, 31, 32, 31, 30, 30, 30, 29, 30, 30, 30 } },
            { 2083, new[] { 31, 31, 32, 31, 31, 30, 30, 30, 29, 30, 30, 30 } },
            { 2084, new[] { 31, 31, 32, 31, 31, 30, 30, 30, 29, 30, 30, 30 } },
            { 2085, new[] { 31, 32, 31, 32, 30, 31, 30, 30, 29, 30, 30, 30 } },
            { 2086, new[] { 30, 32, 31, 32, 31, 30, 30, 30, 29, 30, 30, 30 } },
            { 2087, new[] { 31, 31, 32, 31, 31, 31, 30, 30, 29, 30, 30, 30 } },
            { 2088, new[] { 30, 31, 32, 32, 30, 31, 30, 30, 29, 30, 30, 30 } },
            { 2089, new[] { 30, 32, 31, 32, 31, 30, 30, 30, 29, 30, 30, 30 } },
            { 2090, new[] { 30, 32, 31, 32, 31, 30, 30, 30, 29, 30, 30, 30 } }
        };
    }
}
