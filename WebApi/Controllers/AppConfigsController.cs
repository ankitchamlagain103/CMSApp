using Application.AppConfigs;
using Application.AppConfigs.Commands;
using Application.AppConfigs.Dtos;
using Application.AppConfigs.Queries;
using Application.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AppConfigsController : ControllerBase
    {
        private readonly IAppConfigService _appConfigService;

        public AppConfigsController(IAppConfigService appConfigService)
        {
            _appConfigService = appConfigService;
        }

        [HttpPost]
        public async Task<ActionResult<CommonResponse<AppConfigDto>>> CreateAppConfig([FromBody] CreateAppConfigCommand command, CancellationToken cancellationToken)
        {
            var response = await _appConfigService.CreateAppConfigAsync(command, cancellationToken);
            if (response.ResponseCode != ResponseCodes.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpGet]
        public async Task<ActionResult<CommonResponse<PaginatedResponse<AppConfigDto>>>> GetAppConfigs([FromQuery] GetAppConfigsQuery query, CancellationToken cancellationToken)
        {
            var response = await _appConfigService.GetAppConfigsAsync(query, cancellationToken);
            return Ok(response);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<CommonResponse<AppConfigDto>>> GetAppConfigById(Guid id, CancellationToken cancellationToken)
        {
            var response = await _appConfigService.GetAppConfigByIdAsync(id, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpGet("group/{configGroup}")]
        public async Task<ActionResult<CommonResponse<List<AppConfigDto>>>> GetAppConfigsByGroup(string configGroup, CancellationToken cancellationToken)
        {
            var response = await _appConfigService.GetAppConfigsByGroupAsync(configGroup, cancellationToken);
            return Ok(response);
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<CommonResponse<AppConfigDto>>> UpdateAppConfig(Guid id, [FromBody] UpdateAppConfigCommand command, CancellationToken cancellationToken)
        {
            var response = await _appConfigService.UpdateAppConfigAsync(id, command, cancellationToken);
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
        public async Task<ActionResult<CommonResponse<bool>>> DeleteAppConfig(Guid id, CancellationToken cancellationToken)
        {
            var response = await _appConfigService.DeleteAppConfigAsync(id, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpGet("public")]
        [AllowAnonymous]
        public async Task<ActionResult<CommonResponse<List<PublicAppConfigDto>>>> GetPublicAppConfigs(CancellationToken cancellationToken)
        {
            var response = await _appConfigService.GetPublicAppConfigsAsync(cancellationToken);
            return Ok(response);
        }
    }
}
