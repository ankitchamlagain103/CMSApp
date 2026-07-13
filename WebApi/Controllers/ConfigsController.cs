using Application.Common.Models;
using Application.Configs;
using Application.Configs.Commands;
using Application.Configs.Dtos;
using Application.Configs.Queries;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConfigsController : ControllerBase
    {
        private readonly IConfigService _configService;

        public ConfigsController(IConfigService configService)
        {
            _configService = configService;
        }

        [HttpPost("types")]
        public async Task<ActionResult<CommonResponse<ConfigTypeDto>>> CreateConfigType([FromBody] CreateConfigTypeCommand command, CancellationToken cancellationToken)
        {
            var response = await _configService.CreateConfigTypeAsync(command, cancellationToken);
            if (response.ResponseCode != ResponseCodes.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpGet("types")]
        public async Task<ActionResult<CommonResponse<PaginatedResponse<ConfigTypeDto>>>> GetConfigTypes([FromQuery] GetConfigTypesQuery query, CancellationToken cancellationToken)
        {
            var response = await _configService.GetConfigTypesAsync(query, cancellationToken);
            return Ok(response);
        }

        [HttpGet("types/{id:guid}")]
        public async Task<ActionResult<CommonResponse<ConfigTypeDto>>> GetConfigTypeById(Guid id, CancellationToken cancellationToken)
        {
            var response = await _configService.GetConfigTypeByIdAsync(id, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpPut("types/{id:guid}")]
        public async Task<ActionResult<CommonResponse<ConfigTypeDto>>> UpdateConfigType(Guid id, [FromBody] UpdateConfigTypeCommand command, CancellationToken cancellationToken)
        {
            var response = await _configService.UpdateConfigTypeAsync(id, command, cancellationToken);
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

        [HttpDelete("types/{id:guid}")]
        public async Task<ActionResult<CommonResponse<bool>>> DeleteConfigType(Guid id, CancellationToken cancellationToken)
        {
            var response = await _configService.DeleteConfigTypeAsync(id, cancellationToken);
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

        [HttpPost]
        public async Task<ActionResult<CommonResponse<ConfigDto>>> CreateConfig([FromBody] CreateConfigCommand command, CancellationToken cancellationToken)
        {
            var response = await _configService.CreateConfigAsync(command, cancellationToken);
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

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<CommonResponse<ConfigDto>>> GetConfigById(Guid id, CancellationToken cancellationToken)
        {
            var response = await _configService.GetConfigByIdAsync(id, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<CommonResponse<ConfigDto>>> UpdateConfig(Guid id, [FromBody] UpdateConfigCommand command, CancellationToken cancellationToken)
        {
            var response = await _configService.UpdateConfigAsync(id, command, cancellationToken);
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
        public async Task<ActionResult<CommonResponse<bool>>> DeleteConfig(Guid id, CancellationToken cancellationToken)
        {
            var response = await _configService.DeleteConfigAsync(id, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpGet("dropdown/{typeCode:int}")]
        public async Task<ActionResult<CommonResponse<List<DropdownItemDto>>>> GetConfigsByTypeCode(int typeCode, CancellationToken cancellationToken)
        {
            var response = await _configService.GetConfigsByTypeCodeAsync(typeCode, cancellationToken);
            return Ok(response);
        }
    }
}
