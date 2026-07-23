using Application.Common.Models;
using Application.PayrollRuns;
using Application.PayrollRuns.Commands;
using Application.PayrollRuns.Dtos;
using Application.PayrollRuns.Queries;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PayrollRunsController : ControllerBase
    {
        private readonly IPayrollRunService _payrollRunService;

        public PayrollRunsController(IPayrollRunService payrollRunService)
        {
            _payrollRunService = payrollRunService;
        }

        [HttpGet]
        public async Task<ActionResult<CommonResponse<PaginatedResponse<PayrollRunDto>>>> GetPayrollRuns([FromQuery] GetPayrollRunsQuery query, CancellationToken cancellationToken)
        {
            var response = await _payrollRunService.GetRunsAsync(query, cancellationToken);
            return Ok(response);
        }

        [HttpPost]
        public async Task<ActionResult<CommonResponse<PayrollGenerationResultDto>>> CreatePayrollRun([FromBody] CreatePayrollRunCommand command, CancellationToken cancellationToken)
        {
            var response = await _payrollRunService.CreateRunAsync(command, cancellationToken);
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

        [HttpPost("{id:guid}/refresh")]
        public async Task<ActionResult<CommonResponse<PayrollGenerationResultDto>>> RefreshRun(Guid id, CancellationToken cancellationToken)
        {
            var response = await _payrollRunService.RefreshRunAsync(id, cancellationToken);
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
        public async Task<ActionResult<CommonResponse<PayrollRunDto>>> GetPayrollRunById(Guid id, CancellationToken cancellationToken)
        {
            var response = await _payrollRunService.GetRunByIdAsync(id, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpPost("{id:guid}/approve")]
        public async Task<ActionResult<CommonResponse<PayrollRunDto>>> ApproveRun(Guid id, CancellationToken cancellationToken)
        {
            var response = await _payrollRunService.ApproveRunAsync(id, cancellationToken);
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

        [HttpPost("{id:guid}/mark-paid")]
        public async Task<ActionResult<CommonResponse<PayrollRunDto>>> MarkPaid(Guid id, CancellationToken cancellationToken)
        {
            var response = await _payrollRunService.MarkPaidAsync(id, cancellationToken);
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
        public async Task<ActionResult<CommonResponse<PayrollRunDto>>> CancelRun(Guid id, CancellationToken cancellationToken)
        {
            var response = await _payrollRunService.CancelRunAsync(id, cancellationToken);
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

        [HttpGet("{id:guid}/slips/{slipId:guid}")]
        public async Task<ActionResult<CommonResponse<SalarySlipDto>>> GetSlipById(Guid id, Guid slipId, CancellationToken cancellationToken)
        {
            var response = await _payrollRunService.GetSlipByIdAsync(id, slipId, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpPost("{id:guid}/slips/{slipId:guid}/cancel")]
        public async Task<ActionResult<CommonResponse<SalarySlipDto>>> CancelSlip(Guid id, Guid slipId, CancellationToken cancellationToken)
        {
            var response = await _payrollRunService.CancelSlipAsync(id, slipId, cancellationToken);
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

        [HttpPost("{id:guid}/slips/{slipId:guid}/approve")]
        public async Task<ActionResult<CommonResponse<SalarySlipDto>>> ApproveSlip(Guid id, Guid slipId, CancellationToken cancellationToken)
        {
            var response = await _payrollRunService.ApproveSlipAsync(id, slipId, cancellationToken);
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

        [HttpPost("{id:guid}/slips/{slipId:guid}/regenerate")]
        public async Task<ActionResult<CommonResponse<SalarySlipDto>>> RegenerateSlip(Guid id, Guid slipId, CancellationToken cancellationToken)
        {
            var response = await _payrollRunService.RegenerateSlipAsync(id, slipId, cancellationToken);
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

        [HttpPost("{id:guid}/slips/{slipId:guid}/lines")]
        public async Task<ActionResult<CommonResponse<SalarySlipDto>>> AddSlipLine(Guid id, Guid slipId, [FromBody] SalarySlipLineInput command, CancellationToken cancellationToken)
        {
            var response = await _payrollRunService.AddSlipLineAsync(id, slipId, command, cancellationToken);
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

        [HttpPut("{id:guid}/slips/{slipId:guid}/lines/{lineId:guid}")]
        public async Task<ActionResult<CommonResponse<SalarySlipDto>>> UpdateSlipLine(Guid id, Guid slipId, Guid lineId, [FromBody] UpdateSalarySlipLineCommand command, CancellationToken cancellationToken)
        {
            var response = await _payrollRunService.UpdateSlipLineAsync(id, slipId, lineId, command, cancellationToken);
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

        [HttpDelete("{id:guid}/slips/{slipId:guid}/lines/{lineId:guid}")]
        public async Task<ActionResult<CommonResponse<SalarySlipDto>>> RemoveSlipLine(Guid id, Guid slipId, Guid lineId, CancellationToken cancellationToken)
        {
            var response = await _payrollRunService.RemoveSlipLineAsync(id, slipId, lineId, cancellationToken);
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
