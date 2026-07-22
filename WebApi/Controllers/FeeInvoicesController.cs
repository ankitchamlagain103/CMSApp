using Application.Common.Models;
using Application.FeeInvoices;
using Application.FeeInvoices.Commands;
using Application.FeeInvoices.Dtos;
using Application.FeeInvoices.Queries;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FeeInvoicesController : ControllerBase
    {
        private readonly IFeeInvoiceService _feeInvoiceService;

        public FeeInvoicesController(IFeeInvoiceService feeInvoiceService)
        {
            _feeInvoiceService = feeInvoiceService;
        }

        [HttpGet]
        public async Task<ActionResult<CommonResponse<PaginatedResponse<FeeInvoiceDto>>>> GetFeeInvoices([FromQuery] GetFeeInvoicesQuery query, CancellationToken cancellationToken)
        {
            var response = await _feeInvoiceService.GetFeeInvoicesAsync(query, cancellationToken);
            return Ok(response);
        }

        [HttpPost("generate")]
        public async Task<ActionResult<CommonResponse<FeeGenerationResultDto>>> Generate([FromBody] GenerateFeeInvoicesCommand command, CancellationToken cancellationToken)
        {
            var response = await _feeInvoiceService.GenerateAsync(command, cancellationToken);
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
        public async Task<ActionResult<CommonResponse<FeeInvoiceDto>>> GetFeeInvoiceById(Guid id, CancellationToken cancellationToken)
        {
            var response = await _feeInvoiceService.GetFeeInvoiceByIdAsync(id, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<CommonResponse<FeeInvoiceDto>>> UpdateFeeInvoice(Guid id, [FromBody] UpdateFeeInvoiceCommand command, CancellationToken cancellationToken)
        {
            var response = await _feeInvoiceService.UpdateFeeInvoiceAsync(id, command, cancellationToken);
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

        [HttpPost("{id:guid}/lines")]
        public async Task<ActionResult<CommonResponse<FeeInvoiceDto>>> AddLine(Guid id, [FromBody] FeeInvoiceLineInput command, CancellationToken cancellationToken)
        {
            var response = await _feeInvoiceService.AddLineAsync(id, command, cancellationToken);
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

        [HttpPut("{id:guid}/lines/{lineId:guid}")]
        public async Task<ActionResult<CommonResponse<FeeInvoiceDto>>> UpdateLine(Guid id, Guid lineId, [FromBody] UpdateFeeInvoiceLineCommand command, CancellationToken cancellationToken)
        {
            var response = await _feeInvoiceService.UpdateLineAsync(id, lineId, command, cancellationToken);
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

        [HttpDelete("{id:guid}/lines/{lineId:guid}")]
        public async Task<ActionResult<CommonResponse<FeeInvoiceDto>>> RemoveLine(Guid id, Guid lineId, CancellationToken cancellationToken)
        {
            var response = await _feeInvoiceService.RemoveLineAsync(id, lineId, cancellationToken);
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

        [HttpPost("{id:guid}/lines/{lineId:guid}/settle-annual-in-full")]
        public async Task<ActionResult<CommonResponse<FeeInvoiceDto>>> SettleAnnualInFull(Guid id, Guid lineId, CancellationToken cancellationToken)
        {
            var response = await _feeInvoiceService.SettleAnnualInFullAsync(id, lineId, cancellationToken);
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

        [HttpPost("finalize")]
        public async Task<ActionResult<CommonResponse<FinalizeResultDto>>> Finalize([FromBody] FinalizeFeeInvoicesCommand command, CancellationToken cancellationToken)
        {
            var response = await _feeInvoiceService.FinalizeAsync(command, cancellationToken);
            if (response.ResponseCode != ResponseCodes.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpPost("{id:guid}/unfinalize")]
        public async Task<ActionResult<CommonResponse<FeeInvoiceDto>>> Unfinalize(Guid id, CancellationToken cancellationToken)
        {
            var response = await _feeInvoiceService.UnfinalizeAsync(id, cancellationToken);
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

        [HttpPost("{id:guid}/cancel")]
        public async Task<ActionResult<CommonResponse<FeeInvoiceDto>>> Cancel(Guid id, CancellationToken cancellationToken)
        {
            var response = await _feeInvoiceService.CancelAsync(id, cancellationToken);
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

        [HttpGet("statement/{enrollmentId:guid}")]
        public async Task<ActionResult<CommonResponse<FeeStatementDto>>> GetStatement(Guid enrollmentId, CancellationToken cancellationToken)
        {
            var response = await _feeInvoiceService.GetStatementAsync(enrollmentId, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpGet("account-statement/{enrollmentId:guid}")]
        public async Task<ActionResult<CommonResponse<FeeAccountStatementDto>>> GetAccountStatement(Guid enrollmentId, CancellationToken cancellationToken)
        {
            var response = await _feeInvoiceService.GetAccountStatementAsync(enrollmentId, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpGet("students")]
        public async Task<ActionResult<CommonResponse<PaginatedResponse<FeeStudentSearchResultDto>>>> SearchStudents([FromQuery] SearchFeeStudentsQuery query, CancellationToken cancellationToken)
        {
            var response = await _feeInvoiceService.SearchStudentsAsync(query, cancellationToken);
            return Ok(response);
        }

        [HttpGet("adjustments")]
        public async Task<ActionResult<CommonResponse<List<FeeAdjustmentDto>>>> GetAdjustments([FromQuery] GetFeeAdjustmentsQuery query, CancellationToken cancellationToken)
        {
            var response = await _feeInvoiceService.GetAdjustmentsAsync(query, cancellationToken);
            return Ok(response);
        }

        [HttpPost("adjustments")]
        public async Task<ActionResult<CommonResponse<FeeAdjustmentDto>>> CreateAdjustment([FromBody] CreateFeeAdjustmentCommand command, CancellationToken cancellationToken)
        {
            var response = await _feeInvoiceService.CreateAdjustmentAsync(command, cancellationToken);
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

        [HttpPost("adjustments/bulk")]
        public async Task<ActionResult<CommonResponse<BulkFeeAdjustmentResultDto>>> CreateBulkAdjustment([FromBody] CreateBulkFeeAdjustmentCommand command, CancellationToken cancellationToken)
        {
            var response = await _feeInvoiceService.CreateBulkAdjustmentAsync(command, cancellationToken);
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

        [HttpPut("adjustments/{adjustmentId:guid}")]
        public async Task<ActionResult<CommonResponse<FeeAdjustmentDto>>> UpdateAdjustment(Guid adjustmentId, [FromBody] UpdateFeeAdjustmentCommand command, CancellationToken cancellationToken)
        {
            var response = await _feeInvoiceService.UpdateAdjustmentAsync(adjustmentId, command, cancellationToken);
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

        [HttpDelete("adjustments/{adjustmentId:guid}")]
        public async Task<ActionResult<CommonResponse<bool>>> CancelAdjustment(Guid adjustmentId, CancellationToken cancellationToken)
        {
            var response = await _feeInvoiceService.CancelAdjustmentAsync(adjustmentId, cancellationToken);
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
