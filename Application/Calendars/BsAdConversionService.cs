using Application.Common.Interfaces;

namespace Application.Calendars
{
    // Cumulative day-offset conversion between AD and BS, anchored at BS 2000-01-01
    // (Baisakh 1, 2000) == AD 1943-04-14. (The design doc said 1943-04-13; that is off by one
    // -- verified against 10 known BS/AD checkpoints incl. Nepali New Year 2072/2081/2082/2083
    // and Constitution Day 2072-06-03 = 2015-09-20 with the seeded month-length table.)
    // Month lengths come from the BsMonthLength table and are loaded ONCE per service
    // instance (scoped => once per request) -- the month-view endpoint converts every day of
    // a month, so per-call queries would be 30+ round trips.
    public class BsAdConversionService : IBsAdConversionService
    {
        private static readonly DateTime AnchorAdDate = new DateTime(1943, 4, 14);
        private const int AnchorBsYear = 2000;

        private readonly IUnitOfWork _unitOfWork;
        private Dictionary<int, int[]> _monthLengthsByYear;

        public BsAdConversionService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<int> GetDaysInBsMonthAsync(int bsYear, int bsMonth, CancellationToken cancellationToken = default)
        {
            ValidateBsMonth(bsMonth);

            var monthLengths = await GetMonthLengthsAsync(cancellationToken);
            if (!monthLengths.TryGetValue(bsYear, out var yearMonths) || yearMonths[bsMonth - 1] <= 0)
            {
                throw new BsCalendarException("BS month-length configuration is missing for BS year " + bsYear + ", month " + bsMonth + ". Please update the calendar configuration.");
            }

            return yearMonths[bsMonth - 1];
        }

        public async Task<DateTime> ConvertBsToAdAsync(int bsYear, int bsMonth, int bsDay, CancellationToken cancellationToken = default)
        {
            ValidateBsMonth(bsMonth);

            if (bsYear < AnchorBsYear)
            {
                throw new BsCalendarException("Conversions prior to BS " + AnchorBsYear + " are not supported.");
            }

            if (bsDay < 1)
            {
                throw new BsCalendarException("BS day must be 1 or greater.");
            }

            var monthLengths = await GetMonthLengthsAsync(cancellationToken);
            long totalDaysOffset = 0;

            // Days in fully elapsed BS years between the anchor and the target year.
            for (var year = AnchorBsYear; year < bsYear; year++)
            {
                var yearMonths = GetConfiguredYear(monthLengths, year);
                for (var monthIndex = 0; monthIndex < 12; monthIndex++)
                {
                    totalDaysOffset += yearMonths[monthIndex];
                }
            }

            // Days in fully elapsed months of the target year.
            var targetYearMonths = GetConfiguredYear(monthLengths, bsYear);
            for (var month = 1; month < bsMonth; month++)
            {
                totalDaysOffset += targetYearMonths[month - 1];
            }

            var daysInTargetMonth = targetYearMonths[bsMonth - 1];
            if (bsDay > daysInTargetMonth)
            {
                throw new BsCalendarException("BS " + bsYear + "-" + bsMonth + " has only " + daysInTargetMonth + " days (received day " + bsDay + ").");
            }

            totalDaysOffset += bsDay - 1;

            var adDate = AnchorAdDate.AddDays(totalDaysOffset);
            return adDate;
        }

        public async Task<(int BsYear, int BsMonth, int BsDay)> ConvertAdToBsAsync(DateTime adDate, CancellationToken cancellationToken = default)
        {
            var targetAdDate = adDate.Date;
            if (targetAdDate < AnchorAdDate)
            {
                throw new BsCalendarException("Conversions prior to AD " + AnchorAdDate.ToString("yyyy-MM-dd") + " are not supported.");
            }

            var monthLengths = await GetMonthLengthsAsync(cancellationToken);
            var remainingDays = (int)(targetAdDate - AnchorAdDate).TotalDays;

            var currentBsYear = AnchorBsYear;
            var currentBsMonth = 1;

            while (true)
            {
                var yearMonths = GetConfiguredYear(monthLengths, currentBsYear);
                var daysInCurrentMonth = yearMonths[currentBsMonth - 1];

                if (remainingDays < daysInCurrentMonth)
                {
                    break;
                }

                remainingDays -= daysInCurrentMonth;
                if (currentBsMonth == 12)
                {
                    currentBsMonth = 1;
                    currentBsYear++;
                }
                else
                {
                    currentBsMonth++;
                }
            }

            var currentBsDay = remainingDays + 1;
            return (currentBsYear, currentBsMonth, currentBsDay);
        }

        private async Task<Dictionary<int, int[]>> GetMonthLengthsAsync(CancellationToken cancellationToken)
        {
            if (_monthLengthsByYear != null)
            {
                return _monthLengthsByYear;
            }

            var configuredLengths = await _unitOfWork.CalendarConfigs.GetAllMonthLengthsOrderedAsync(cancellationToken);

            var lengthsByYear = new Dictionary<int, int[]>();
            foreach (var monthLength in configuredLengths)
            {
                if (monthLength.BsMonth < 1 || monthLength.BsMonth > 12)
                {
                    continue;
                }

                if (!lengthsByYear.TryGetValue(monthLength.BsYear, out var yearMonths))
                {
                    yearMonths = new int[12];
                    lengthsByYear[monthLength.BsYear] = yearMonths;
                }

                yearMonths[monthLength.BsMonth - 1] = monthLength.DaysInMonth;
            }

            _monthLengthsByYear = lengthsByYear;
            return _monthLengthsByYear;
        }

        private static int[] GetConfiguredYear(Dictionary<int, int[]> monthLengths, int bsYear)
        {
            if (!monthLengths.TryGetValue(bsYear, out var yearMonths))
            {
                throw new BsCalendarException("BS month-length configuration is missing for BS year " + bsYear + ". Please update the calendar configuration.");
            }

            for (var monthIndex = 0; monthIndex < 12; monthIndex++)
            {
                if (yearMonths[monthIndex] <= 0)
                {
                    throw new BsCalendarException("BS month-length configuration for BS year " + bsYear + " is incomplete (month " + (monthIndex + 1) + " is missing).");
                }
            }

            return yearMonths;
        }

        private static void ValidateBsMonth(int bsMonth)
        {
            if (bsMonth < 1 || bsMonth > 12)
            {
                throw new BsCalendarException("BS month must be between 1 (Baisakh) and 12 (Chaitra).");
            }
        }
    }
}
