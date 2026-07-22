using Application.Common.Models;
using Application.Meetings;
using Application.Meetings.Commands;
using Application.Meetings.Dtos;
using Application.Meetings.Queries;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/meetings")]
    public class MeetingsController : ControllerBase
    {
        private readonly IMeetingService _meetingService;

        public MeetingsController(IMeetingService meetingService)
        {
            _meetingService = meetingService;
        }

        [HttpGet]
        public async Task<ActionResult<CommonResponse<PaginatedResponse<MeetingDto>>>> GetMeetings([FromQuery] GetMeetingsQuery query, CancellationToken cancellationToken)
        {
            var response = await _meetingService.GetMeetingsAsync(query, cancellationToken);
            return Ok(response);
        }

        [HttpPost("schedule")]
        public async Task<ActionResult<CommonResponse<MeetingDto>>> ScheduleMeeting([FromBody] ScheduleMeetingCommand command, CancellationToken cancellationToken)
        {
            var response = await _meetingService.ScheduleMeetingAsync(command, cancellationToken);
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

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<CommonResponse<MeetingDto>>> GetMeetingById(Guid id, CancellationToken cancellationToken)
        {
            var response = await _meetingService.GetMeetingByIdAsync(id, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<CommonResponse<MeetingDto>>> UpdateMeeting(Guid id, [FromBody] UpdateMeetingCommand command, CancellationToken cancellationToken)
        {
            var response = await _meetingService.UpdateMeetingAsync(id, command, cancellationToken);
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

        [HttpDelete("{id:guid}")]
        public async Task<ActionResult<CommonResponse<bool>>> DeleteMeeting(Guid id, CancellationToken cancellationToken)
        {
            var response = await _meetingService.DeleteMeetingAsync(id, cancellationToken);
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

        [HttpPost("respond")]
        public async Task<ActionResult<CommonResponse<MeetingDto>>> RespondToInvitation([FromBody] RespondInvitationCommand command, CancellationToken cancellationToken)
        {
            var response = await _meetingService.RespondToInvitationAsync(command, cancellationToken);
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
