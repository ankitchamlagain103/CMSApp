using Application.Common.Models;
using Application.Guardians;
using Application.Guardians.Commands;
using Application.Guardians.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GuardiansController : ControllerBase
    {
        private readonly IGuardianService _guardianService;

        public GuardiansController(IGuardianService guardianService)
        {
            _guardianService = guardianService;
        }

        [HttpPost]
        public async Task<ActionResult<CommonResponse<GuardianDto>>> CreateGuardian([FromBody] CreateGuardianCommand command, CancellationToken cancellationToken)
        {
            var response = await _guardianService.CreateGuardianAsync(command, cancellationToken);
            if (response.ResponseCode != ResponseCodes.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpGet]
        public async Task<ActionResult<CommonResponse<PaginatedResponse<GuardianDto>>>> GetGuardians([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
        {
            var response = await _guardianService.GetGuardiansAsync(page, pageSize, cancellationToken);
            return Ok(response);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<CommonResponse<GuardianDto>>> GetGuardianById(Guid id, CancellationToken cancellationToken)
        {
            var response = await _guardianService.GetGuardianByIdAsync(id, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<CommonResponse<GuardianDto>>> UpdateGuardian(Guid id, [FromBody] UpdateGuardianCommand command, CancellationToken cancellationToken)
        {
            var response = await _guardianService.UpdateGuardianAsync(id, command, cancellationToken);
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
        public async Task<ActionResult<CommonResponse<bool>>> DeleteGuardian(Guid id, CancellationToken cancellationToken)
        {
            var response = await _guardianService.DeleteGuardianAsync(id, cancellationToken);
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
