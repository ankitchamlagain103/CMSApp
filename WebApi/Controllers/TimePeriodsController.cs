using Application.Common.Models;
using Application.TimePeriods;
using Application.TimePeriods.Commands;
using Application.TimePeriods.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TimePeriodsController : ControllerBase
    {
        private readonly ITimePeriodService _timePeriodService;

        public TimePeriodsController(ITimePeriodService timePeriodService)
        {
            _timePeriodService = timePeriodService;
        }

        [HttpPost]
        public async Task<ActionResult<CommonResponse<TimePeriodDto>>> CreateTimePeriod([FromBody] CreateTimePeriodCommand command, CancellationToken cancellationToken)
        {
            var response = await _timePeriodService.CreateTimePeriodAsync(command, cancellationToken);
            if (response.ResponseCode != ResponseCodes.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpGet]
        public async Task<ActionResult<CommonResponse<PaginatedResponse<TimePeriodDto>>>> GetTimePeriods([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
        {
            var response = await _timePeriodService.GetTimePeriodsAsync(page, pageSize, cancellationToken);
            return Ok(response);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<CommonResponse<TimePeriodDto>>> GetTimePeriodById(Guid id, CancellationToken cancellationToken)
        {
            var response = await _timePeriodService.GetTimePeriodByIdAsync(id, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<CommonResponse<TimePeriodDto>>> UpdateTimePeriod(Guid id, [FromBody] UpdateTimePeriodCommand command, CancellationToken cancellationToken)
        {
            var response = await _timePeriodService.UpdateTimePeriodAsync(id, command, cancellationToken);
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
        public async Task<ActionResult<CommonResponse<bool>>> DeleteTimePeriod(Guid id, CancellationToken cancellationToken)
        {
            var response = await _timePeriodService.DeleteTimePeriodAsync(id, cancellationToken);
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

        // Bulk class<->period mapping -- the cross product of every submitted class and every
        // submitted period, skip-list style. This is the "certain classes run different periods"
        // input: map one period set to one group of classes, a different set to another group,
        // via two separate calls.
        [HttpPost("map")]
        public async Task<ActionResult<CommonResponse<ClassTimePeriodMapResultDto>>> MapClassTimePeriods([FromBody] MapClassTimePeriodsCommand command, CancellationToken cancellationToken)
        {
            var response = await _timePeriodService.MapClassTimePeriodsAsync(command, cancellationToken);
            if (response.ResponseCode != ResponseCodes.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpGet("map/{academicClassId:guid}")]
        public async Task<ActionResult<CommonResponse<List<ClassTimePeriodDto>>>> GetClassTimePeriods(Guid academicClassId, CancellationToken cancellationToken)
        {
            var response = await _timePeriodService.GetClassTimePeriodsAsync(academicClassId, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpDelete("map/{academicClassId:guid}/{timePeriodId:guid}")]
        public async Task<ActionResult<CommonResponse<bool>>> UnmapClassTimePeriod(Guid academicClassId, Guid timePeriodId, CancellationToken cancellationToken)
        {
            var response = await _timePeriodService.UnmapClassTimePeriodAsync(academicClassId, timePeriodId, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }
    }
}
