using Application.Common.Models;
using Application.Payroll.FiscalYears;
using Application.Payroll.FiscalYears.Commands;
using Application.Payroll.FiscalYears.Dtos;
using Application.Payroll.FiscalYears.Queries;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FiscalYearsController : ControllerBase
    {
        private readonly IFiscalYearService _fiscalYearService;

        public FiscalYearsController(IFiscalYearService fiscalYearService)
        {
            _fiscalYearService = fiscalYearService;
        }

        [HttpPost]
        public async Task<ActionResult<CommonResponse<FiscalYearDto>>> CreateFiscalYear([FromBody] CreateFiscalYearCommand command, CancellationToken cancellationToken)
        {
            var response = await _fiscalYearService.CreateFiscalYearAsync(command, cancellationToken);
            if (response.ResponseCode != ResponseCodes.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpGet]
        public async Task<ActionResult<CommonResponse<PaginatedResponse<FiscalYearDto>>>> GetFiscalYears([FromQuery] GetFiscalYearsQuery query, CancellationToken cancellationToken)
        {
            var response = await _fiscalYearService.GetFiscalYearsAsync(query, cancellationToken);
            return Ok(response);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<CommonResponse<FiscalYearDto>>> GetFiscalYearById(Guid id, CancellationToken cancellationToken)
        {
            var response = await _fiscalYearService.GetFiscalYearByIdAsync(id, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<CommonResponse<FiscalYearDto>>> UpdateFiscalYear(Guid id, [FromBody] UpdateFiscalYearCommand command, CancellationToken cancellationToken)
        {
            var response = await _fiscalYearService.UpdateFiscalYearAsync(id, command, cancellationToken);
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
        public async Task<ActionResult<CommonResponse<bool>>> DeleteFiscalYear(Guid id, CancellationToken cancellationToken)
        {
            var response = await _fiscalYearService.DeleteFiscalYearAsync(id, cancellationToken);
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

        [HttpPost("{id:guid}/taxslabs")]
        public async Task<ActionResult<CommonResponse<TaxSlabDto>>> AddTaxSlab(Guid id, [FromBody] CreateTaxSlabCommand command, CancellationToken cancellationToken)
        {
            var response = await _fiscalYearService.AddTaxSlabAsync(id, command, cancellationToken);
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

        [HttpGet("{id:guid}/taxslabs")]
        public async Task<ActionResult<CommonResponse<List<TaxSlabDto>>>> GetTaxSlabs(Guid id, CancellationToken cancellationToken)
        {
            var response = await _fiscalYearService.GetTaxSlabsAsync(id, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpPut("{id:guid}/taxslabs/{taxSlabId:guid}")]
        public async Task<ActionResult<CommonResponse<TaxSlabDto>>> UpdateTaxSlab(Guid id, Guid taxSlabId, [FromBody] UpdateTaxSlabCommand command, CancellationToken cancellationToken)
        {
            var response = await _fiscalYearService.UpdateTaxSlabAsync(id, taxSlabId, command, cancellationToken);
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

        [HttpDelete("{id:guid}/taxslabs/{taxSlabId:guid}")]
        public async Task<ActionResult<CommonResponse<bool>>> RemoveTaxSlab(Guid id, Guid taxSlabId, CancellationToken cancellationToken)
        {
            var response = await _fiscalYearService.RemoveTaxSlabAsync(id, taxSlabId, cancellationToken);
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
