using Application.Common.Models;
using Application.Fees;
using Application.Fees.Commands;
using Application.Fees.Dtos;
using Application.Fees.Queries;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FeeStructuresController : ControllerBase
    {
        private readonly IFeeStructureService _feeStructureService;

        public FeeStructuresController(IFeeStructureService feeStructureService)
        {
            _feeStructureService = feeStructureService;
        }

        [HttpPost]
        public async Task<ActionResult<CommonResponse<FeeStructureDto>>> CreateFeeStructure([FromBody] CreateFeeStructureCommand command, CancellationToken cancellationToken)
        {
            var response = await _feeStructureService.CreateFeeStructureAsync(command, cancellationToken);
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

        [HttpGet]
        public async Task<ActionResult<CommonResponse<PaginatedResponse<FeeStructureDto>>>> GetFeeStructures([FromQuery] GetFeeStructuresQuery query, CancellationToken cancellationToken)
        {
            var response = await _feeStructureService.GetFeeStructuresAsync(query, cancellationToken);
            return Ok(response);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<CommonResponse<FeeStructureDto>>> GetFeeStructureById(Guid id, CancellationToken cancellationToken)
        {
            var response = await _feeStructureService.GetFeeStructureByIdAsync(id, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<CommonResponse<FeeStructureDto>>> UpdateFeeStructure(Guid id, [FromBody] UpdateFeeStructureCommand command, CancellationToken cancellationToken)
        {
            var response = await _feeStructureService.UpdateFeeStructureAsync(id, command, cancellationToken);
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
        public async Task<ActionResult<CommonResponse<bool>>> DeleteFeeStructure(Guid id, CancellationToken cancellationToken)
        {
            var response = await _feeStructureService.DeleteFeeStructureAsync(id, cancellationToken);
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

        [HttpPost("{id:guid}/items")]
        public async Task<ActionResult<CommonResponse<FeeStructureItemDto>>> AddItem(Guid id, [FromBody] FeeStructureItemInput command, CancellationToken cancellationToken)
        {
            var response = await _feeStructureService.AddItemAsync(id, command, cancellationToken);
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

        [HttpPut("{id:guid}/items/{itemId:guid}")]
        public async Task<ActionResult<CommonResponse<FeeStructureItemDto>>> UpdateItem(Guid id, Guid itemId, [FromBody] UpdateFeeStructureItemCommand command, CancellationToken cancellationToken)
        {
            var response = await _feeStructureService.UpdateItemAsync(id, itemId, command, cancellationToken);
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

        [HttpDelete("{id:guid}/items/{itemId:guid}")]
        public async Task<ActionResult<CommonResponse<bool>>> RemoveItem(Guid id, Guid itemId, CancellationToken cancellationToken)
        {
            var response = await _feeStructureService.RemoveItemAsync(id, itemId, cancellationToken);
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
