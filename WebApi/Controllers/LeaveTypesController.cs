using Application.Common.Models;
using Application.LeaveTypes;
using Application.LeaveTypes.Commands;
using Application.LeaveTypes.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LeaveTypesController : ControllerBase
    {
        private readonly ILeaveTypeService _leaveTypeService;

        public LeaveTypesController(ILeaveTypeService leaveTypeService)
        {
            _leaveTypeService = leaveTypeService;
        }

        [HttpPost]
        public async Task<ActionResult<CommonResponse<LeaveTypeDto>>> CreateLeaveType([FromBody] CreateLeaveTypeCommand command, CancellationToken cancellationToken)
        {
            var response = await _leaveTypeService.CreateLeaveTypeAsync(command, cancellationToken);
            if (response.ResponseCode != ResponseCodes.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpGet]
        public async Task<ActionResult<CommonResponse<PaginatedResponse<LeaveTypeDto>>>> GetLeaveTypes([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
        {
            var response = await _leaveTypeService.GetLeaveTypesAsync(page, pageSize, cancellationToken);
            return Ok(response);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<CommonResponse<LeaveTypeDto>>> GetLeaveTypeById(Guid id, CancellationToken cancellationToken)
        {
            var response = await _leaveTypeService.GetLeaveTypeByIdAsync(id, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<CommonResponse<LeaveTypeDto>>> UpdateLeaveType(Guid id, [FromBody] UpdateLeaveTypeCommand command, CancellationToken cancellationToken)
        {
            var response = await _leaveTypeService.UpdateLeaveTypeAsync(id, command, cancellationToken);
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

        [HttpDelete("{id:guid}")]
        public async Task<ActionResult<CommonResponse<bool>>> DeleteLeaveType(Guid id, CancellationToken cancellationToken)
        {
            var response = await _leaveTypeService.DeleteLeaveTypeAsync(id, cancellationToken);
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
