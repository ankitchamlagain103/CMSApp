using Application.Calendars;
using Application.Calendars.Commands;
using Application.Calendars.Dtos;
using Application.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    // BS calendar reference-data administration: yearly month-length upserts, localized
    // month/weekday names, and the weekly-holiday flag.
    [ApiController]
    [Route("api/calendar-configuration")]
    public class CalendarConfigurationController : ControllerBase
    {
        private readonly ICalendarService _calendarService;

        public CalendarConfigurationController(ICalendarService calendarService)
        {
            _calendarService = calendarService;
        }

        [HttpGet("bs-month-lengths")]
        public async Task<ActionResult<CommonResponse<List<BsMonthLengthDto>>>> GetBsMonthLengths([FromQuery] int? bsYear, CancellationToken cancellationToken)
        {
            var response = await _calendarService.GetBsMonthLengthsAsync(bsYear, cancellationToken);
            return Ok(response);
        }

        [HttpPost("bs-month-lengths")]
        public async Task<ActionResult<CommonResponse<List<BsMonthLengthDto>>>> UpsertBsMonthLengths([FromBody] UpsertBsMonthLengthsCommand command, CancellationToken cancellationToken)
        {
            var response = await _calendarService.UpsertBsMonthLengthsAsync(command, cancellationToken);
            if (response.ResponseCode != ResponseCodes.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpGet("localization-data")]
        public async Task<ActionResult<CommonResponse<CalendarLocalizationDto>>> GetLocalizationData(CancellationToken cancellationToken)
        {
            var response = await _calendarService.GetLocalizationDataAsync(cancellationToken);
            return Ok(response);
        }

        [HttpPut("weekdays/{weekdayIndex:int}")]
        public async Task<ActionResult<CommonResponse<BsWeekdayNameDto>>> UpdateWeekday(int weekdayIndex, [FromBody] UpdateBsWeekdayCommand command, CancellationToken cancellationToken)
        {
            var response = await _calendarService.UpdateWeekdayAsync(weekdayIndex, command, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            if (response.ResponseCode != ResponseCodes.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
    }
}
