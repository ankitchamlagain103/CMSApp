using Application.Common.Models;
using Application.FeePayments;
using Application.FeePayments.Commands;
using Application.FeePayments.Dtos;
using Application.FeePayments.Queries;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FeePaymentsController : ControllerBase
    {
        private readonly IFeePaymentService _feePaymentService;

        public FeePaymentsController(IFeePaymentService feePaymentService)
        {
            _feePaymentService = feePaymentService;
        }

        [HttpGet]
        public async Task<ActionResult<CommonResponse<PaginatedResponse<FeePaymentDto>>>> GetPayments([FromQuery] GetFeePaymentsQuery query, CancellationToken cancellationToken)
        {
            var response = await _feePaymentService.GetPaymentsAsync(query, cancellationToken);
            return Ok(response);
        }

        [HttpGet("advance-quote")]
        public async Task<ActionResult<CommonResponse<FeeAdvanceQuoteDto>>> GetAdvanceQuote([FromQuery] GetFeeAdvanceQuoteQuery query, CancellationToken cancellationToken)
        {
            var response = await _feePaymentService.GetAdvanceQuoteAsync(query, cancellationToken);
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

        [HttpPost("preview")]
        public async Task<ActionResult<CommonResponse<FeePaymentPreviewDto>>> Preview([FromBody] CreateFeePaymentCommand command, CancellationToken cancellationToken)
        {
            var response = await _feePaymentService.PreviewAsync(command, cancellationToken);
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
        public async Task<ActionResult<CommonResponse<FeePaymentDto>>> CreatePayment([FromBody] CreateFeePaymentCommand command, CancellationToken cancellationToken)
        {
            var response = await _feePaymentService.CreateAsync(command, cancellationToken);
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
        public async Task<ActionResult<CommonResponse<FeePaymentDto>>> GetPaymentById(Guid id, CancellationToken cancellationToken)
        {
            var response = await _feePaymentService.GetPaymentByIdAsync(id, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpGet("{id:guid}/receipt")]
        public async Task<ActionResult<CommonResponse<DocumentPreviewDto>>> GetReceipt(Guid id, CancellationToken cancellationToken)
        {
            var response = await _feePaymentService.GetReceiptAsync(id, cancellationToken);
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

        [HttpPost("{id:guid}/void")]
        public async Task<ActionResult<CommonResponse<FeePaymentDto>>> VoidPayment(Guid id, CancellationToken cancellationToken)
        {
            var response = await _feePaymentService.VoidAsync(id, cancellationToken);
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
