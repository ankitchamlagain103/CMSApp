using Application.Common.Models;
using Application.Payroll.SalaryCalculations;
using Application.Payroll.SalaryCalculations.Commands;
using Application.Payroll.SalaryCalculations.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SalaryCalculatorController : ControllerBase
    {
        private readonly ISalaryCalculatorService _salaryCalculatorService;

        public SalaryCalculatorController(ISalaryCalculatorService salaryCalculatorService)
        {
            _salaryCalculatorService = salaryCalculatorService;
        }

        [HttpPost]
        public async Task<ActionResult<CommonResponse<SalaryStructureCalculationDto>>> CalculateSalaryStructure([FromBody] CalculateSalaryStructureCommand command, CancellationToken cancellationToken)
        {
            var response = await _salaryCalculatorService.CalculateStructureAsync(command, cancellationToken);
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

        [HttpPost("assign")]
        public async Task<ActionResult<CommonResponse<SalaryStructureAssignResultDto>>> AssignSalaryStructure([FromBody] AssignSalaryStructureCommand command, CancellationToken cancellationToken)
        {
            var response = await _salaryCalculatorService.AssignStructureAsync(command, cancellationToken);
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
