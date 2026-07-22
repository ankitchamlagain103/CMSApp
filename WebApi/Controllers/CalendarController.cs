using Application.Calendars;
using Application.Calendars.Commands;
using Application.Calendars.Dtos;
using Application.Calendars.Queries;
using Application.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    // The unified dual-calendar surface: month view (BS or AD), AD<->BS conversion
    // utilities, calendar events (notes/holidays), and festival occurrences.
    [ApiController]
    [Route("api/calendar")]
    public class CalendarController : ControllerBase
    {
        private readonly ICalendarService _calendarService;

        public CalendarController(ICalendarService calendarService)
        {
            _calendarService = calendarService;
        }

        // --- Month view & conversion utilities ---

        [HttpGet("month-view")]
        public async Task<ActionResult<CommonResponse<CalendarMonthViewDto>>> GetMonthView([FromQuery] GetMonthViewQuery query, CancellationToken cancellationToken)
        {
            var response = await _calendarService.GetMonthViewAsync(query, cancellationToken);
            if (response.ResponseCode != ResponseCodes.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpGet("today")]
        public async Task<ActionResult<CommonResponse<DualDateDto>>> GetToday(CancellationToken cancellationToken)
        {
            var response = await _calendarService.GetTodayAsync(cancellationToken);
            if (response.ResponseCode != ResponseCodes.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpGet("convert/ad-to-bs")]
        public async Task<ActionResult<CommonResponse<DualDateDto>>> ConvertAdToBs([FromQuery] DateTime adDate, CancellationToken cancellationToken)
        {
            var response = await _calendarService.ConvertAdToBsAsync(adDate, cancellationToken);
            if (response.ResponseCode != ResponseCodes.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpGet("convert/bs-to-ad")]
        public async Task<ActionResult<CommonResponse<DualDateDto>>> ConvertBsToAd([FromQuery] int bsYear, [FromQuery] int bsMonth, [FromQuery] int bsDay, CancellationToken cancellationToken)
        {
            var response = await _calendarService.ConvertBsToAdAsync(bsYear, bsMonth, bsDay, cancellationToken);
            if (response.ResponseCode != ResponseCodes.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        // --- Calendar events ---

        [HttpGet("events")]
        public async Task<ActionResult<CommonResponse<PaginatedResponse<CalendarEventDto>>>> GetCalendarEvents([FromQuery] GetCalendarEventsQuery query, CancellationToken cancellationToken)
        {
            var response = await _calendarService.GetCalendarEventsAsync(query, cancellationToken);
            return Ok(response);
        }

        [HttpPost("events")]
        public async Task<ActionResult<CommonResponse<CalendarEventDto>>> CreateCalendarEvent([FromBody] CreateCalendarEventCommand command, CancellationToken cancellationToken)
        {
            var response = await _calendarService.CreateCalendarEventAsync(command, cancellationToken);
            if (response.ResponseCode != ResponseCodes.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpGet("events/{id:guid}")]
        public async Task<ActionResult<CommonResponse<CalendarEventDto>>> GetCalendarEventById(Guid id, CancellationToken cancellationToken)
        {
            var response = await _calendarService.GetCalendarEventByIdAsync(id, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpPut("events/{id:guid}")]
        public async Task<ActionResult<CommonResponse<CalendarEventDto>>> UpdateCalendarEvent(Guid id, [FromBody] UpdateCalendarEventCommand command, CancellationToken cancellationToken)
        {
            var response = await _calendarService.UpdateCalendarEventAsync(id, command, cancellationToken);
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

        [HttpDelete("events/{id:guid}")]
        public async Task<ActionResult<CommonResponse<bool>>> DeleteCalendarEvent(Guid id, CancellationToken cancellationToken)
        {
            var response = await _calendarService.DeleteCalendarEventAsync(id, cancellationToken);
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

        // --- Festival occurrences ---

        [HttpGet("festivals")]
        public async Task<ActionResult<CommonResponse<List<FestivalOccurrenceDto>>>> GetFestivals([FromQuery] int? bsYear, [FromQuery] bool? isActive, CancellationToken cancellationToken)
        {
            var response = await _calendarService.GetFestivalsAsync(bsYear, isActive, cancellationToken);
            return Ok(response);
        }

        [HttpPost("festivals")]
        public async Task<ActionResult<CommonResponse<FestivalOccurrenceDto>>> CreateFestival([FromBody] CreateFestivalOccurrenceCommand command, CancellationToken cancellationToken)
        {
            var response = await _calendarService.CreateFestivalAsync(command, cancellationToken);
            if (response.ResponseCode == ResponseCodes.Conflict)
            {
                return Conflict(response);
            }

            if (response.ResponseCode != ResponseCodes.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpGet("festivals/{id:guid}")]
        public async Task<ActionResult<CommonResponse<FestivalOccurrenceDto>>> GetFestivalById(Guid id, CancellationToken cancellationToken)
        {
            var response = await _calendarService.GetFestivalByIdAsync(id, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpPut("festivals/{id:guid}")]
        public async Task<ActionResult<CommonResponse<FestivalOccurrenceDto>>> UpdateFestival(Guid id, [FromBody] UpdateFestivalOccurrenceCommand command, CancellationToken cancellationToken)
        {
            var response = await _calendarService.UpdateFestivalAsync(id, command, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            if (response.ResponseCode == ResponseCodes.Conflict)
            {
                return Conflict(response);
            }

            if (response.ResponseCode != ResponseCodes.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpDelete("festivals/{id:guid}")]
        public async Task<ActionResult<CommonResponse<bool>>> DeleteFestival(Guid id, CancellationToken cancellationToken)
        {
            var response = await _calendarService.DeleteFestivalAsync(id, cancellationToken);
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
