using Application.Common.Models;
using Application.FeeGenerationRuns;
using Application.FeeGenerationRuns.Dtos;
using Application.FeeGenerationRuns.Queries;
using Application.FeeInvoices.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FeeGenerationRunsController : ControllerBase
    {
        private readonly IFeeGenerationRunService _feeGenerationRunService;

        public FeeGenerationRunsController(IFeeGenerationRunService feeGenerationRunService)
        {
            _feeGenerationRunService = feeGenerationRunService;
        }

        [HttpGet]
        public async Task<ActionResult<CommonResponse<PaginatedResponse<FeeGenerationRunDto>>>> GetFeeGenerationRuns([FromQuery] GetFeeGenerationRunsQuery query, CancellationToken cancellationToken)
        {
            var response = await _feeGenerationRunService.GetFeeGenerationRunsAsync(query, cancellationToken);
            return Ok(response);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<CommonResponse<FeeGenerationRunDetailDto>>> GetFeeGenerationRunById(Guid id, CancellationToken cancellationToken)
        {
            var response = await _feeGenerationRunService.GetFeeGenerationRunByIdAsync(id, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpGet("{id:guid}/classes/{academicClassId:guid}")]
        public async Task<ActionResult<CommonResponse<FeeGenerationClassGroupDto>>> GetFeeGenerationRunClassDetail(Guid id, Guid academicClassId, CancellationToken cancellationToken)
        {
            var response = await _feeGenerationRunService.GetFeeGenerationRunClassDetailAsync(id, academicClassId, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpPost("{id:guid}/refresh")]
        public async Task<ActionResult<CommonResponse<FeeGenerationResultDto>>> RefreshRun(Guid id, CancellationToken cancellationToken)
        {
            var response = await _feeGenerationRunService.RefreshRunAsync(id, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpPost("{id:guid}/classes/{academicClassId:guid}/refresh")]
        public async Task<ActionResult<CommonResponse<FeeGenerationResultDto>>> RefreshRunClass(Guid id, Guid academicClassId, CancellationToken cancellationToken)
        {
            var response = await _feeGenerationRunService.RefreshRunClassAsync(id, academicClassId, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }
    }
}
