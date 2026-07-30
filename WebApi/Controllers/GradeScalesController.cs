using Application.Common.Models;
using Application.GradeScales;
using Application.GradeScales.Commands;
using Application.GradeScales.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GradeScalesController : ControllerBase
    {
        private readonly IGradeScaleService _gradeScaleService;

        public GradeScalesController(IGradeScaleService gradeScaleService)
        {
            _gradeScaleService = gradeScaleService;
        }

        [HttpPost]
        public async Task<ActionResult<CommonResponse<GradeScaleDto>>> CreateGradeScale([FromBody] CreateGradeScaleCommand command, CancellationToken cancellationToken)
        {
            var response = await _gradeScaleService.CreateGradeScaleAsync(command, cancellationToken);
            if (response.ResponseCode == ResponseCodes.Conflict)
            {
                return Conflict(response);
            }

            if (response.ResponseCode != ResponseCodes.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpGet]
        public async Task<ActionResult<CommonResponse<List<GradeScaleDto>>>> GetGradeScales(CancellationToken cancellationToken)
        {
            var response = await _gradeScaleService.GetGradeScalesAsync(cancellationToken);
            return Ok(response);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<CommonResponse<GradeScaleDto>>> GetGradeScaleById(Guid id, CancellationToken cancellationToken)
        {
            var response = await _gradeScaleService.GetGradeScaleByIdAsync(id, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<CommonResponse<GradeScaleDto>>> UpdateGradeScale(Guid id, [FromBody] UpdateGradeScaleCommand command, CancellationToken cancellationToken)
        {
            var response = await _gradeScaleService.UpdateGradeScaleAsync(id, command, cancellationToken);
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
        public async Task<ActionResult<CommonResponse<bool>>> DeleteGradeScale(Guid id, CancellationToken cancellationToken)
        {
            var response = await _gradeScaleService.DeleteGradeScaleAsync(id, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }
    }
}
