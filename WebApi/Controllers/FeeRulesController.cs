using Application.Common.Models;
using Application.FeeRules;
using Application.FeeRules.Commands;
using Application.FeeRules.Dtos;
using Application.FeeRules.Queries;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FeeRulesController : ControllerBase
    {
        private readonly IFeeRuleService _feeRuleService;

        public FeeRulesController(IFeeRuleService feeRuleService)
        {
            _feeRuleService = feeRuleService;
        }

        [HttpGet]
        public async Task<ActionResult<CommonResponse<PaginatedResponse<FeeRuleDto>>>> GetFeeRules([FromQuery] GetFeeRulesQuery query, CancellationToken cancellationToken)
        {
            var response = await _feeRuleService.GetFeeRulesAsync(query, cancellationToken);
            return Ok(response);
        }

        [HttpPost]
        public async Task<ActionResult<CommonResponse<FeeRuleDto>>> CreateFeeRule([FromBody] CreateFeeRuleCommand command, CancellationToken cancellationToken)
        {
            var response = await _feeRuleService.CreateFeeRuleAsync(command, cancellationToken);
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
        public async Task<ActionResult<CommonResponse<FeeRuleDto>>> GetFeeRuleById(Guid id, CancellationToken cancellationToken)
        {
            var response = await _feeRuleService.GetFeeRuleByIdAsync(id, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<CommonResponse<FeeRuleDto>>> UpdateFeeRule(Guid id, [FromBody] UpdateFeeRuleCommand command, CancellationToken cancellationToken)
        {
            var response = await _feeRuleService.UpdateFeeRuleAsync(id, command, cancellationToken);
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
        public async Task<ActionResult<CommonResponse<bool>>> DeleteFeeRule(Guid id, CancellationToken cancellationToken)
        {
            var response = await _feeRuleService.DeleteFeeRuleAsync(id, cancellationToken);
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
