using Application.AcademicYears;
using Application.AcademicYears.Commands;
using Application.AcademicYears.Dtos;
using Application.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AcademicYearsController : ControllerBase
    {
        private readonly IAcademicYearService _academicYearService;

        public AcademicYearsController(IAcademicYearService academicYearService)
        {
            _academicYearService = academicYearService;
        }

        [HttpPost]
        public async Task<ActionResult<CommonResponse<AcademicYearDto>>> CreateAcademicYear([FromBody] CreateAcademicYearCommand command, CancellationToken cancellationToken)
        {
            var response = await _academicYearService.CreateAcademicYearAsync(command, cancellationToken);
            if (response.ResponseCode != ResponseCodes.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpGet]
        public async Task<ActionResult<CommonResponse<PaginatedResponse<AcademicYearDto>>>> GetAcademicYears([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
        {
            var response = await _academicYearService.GetAcademicYearsAsync(page, pageSize, cancellationToken);
            return Ok(response);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<CommonResponse<AcademicYearDto>>> GetAcademicYearById(Guid id, CancellationToken cancellationToken)
        {
            var response = await _academicYearService.GetAcademicYearByIdAsync(id, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<CommonResponse<AcademicYearDto>>> UpdateAcademicYear(Guid id, [FromBody] UpdateAcademicYearCommand command, CancellationToken cancellationToken)
        {
            var response = await _academicYearService.UpdateAcademicYearAsync(id, command, cancellationToken);
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

        [HttpPost("{id:guid}/clone-structure")]
        public async Task<ActionResult<CommonResponse<CloneStructureResultDto>>> CloneStructure(Guid id, [FromBody] CloneYearStructureCommand command, CancellationToken cancellationToken)
        {
            var response = await _academicYearService.CloneStructureAsync(id, command, cancellationToken);
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
        public async Task<ActionResult<CommonResponse<bool>>> DeleteAcademicYear(Guid id, CancellationToken cancellationToken)
        {
            var response = await _academicYearService.DeleteAcademicYearAsync(id, cancellationToken);
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
