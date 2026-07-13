using Application.AcademicClasses;
using Application.AcademicClasses.Commands;
using Application.AcademicClasses.Dtos;
using Application.AcademicClasses.Queries;
using Application.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AcademicClassesController : ControllerBase
    {
        private readonly IAcademicClassService _academicClassService;

        public AcademicClassesController(IAcademicClassService academicClassService)
        {
            _academicClassService = academicClassService;
        }

        [HttpPost]
        public async Task<ActionResult<CommonResponse<AcademicClassDto>>> CreateAcademicClass([FromBody] CreateAcademicClassCommand command, CancellationToken cancellationToken)
        {
            var response = await _academicClassService.CreateAcademicClassAsync(command, cancellationToken);
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
        public async Task<ActionResult<CommonResponse<PaginatedResponse<AcademicClassDto>>>> GetAcademicClasses([FromQuery] GetAcademicClassesQuery query, CancellationToken cancellationToken)
        {
            var response = await _academicClassService.GetAcademicClassesAsync(query, cancellationToken);
            return Ok(response);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<CommonResponse<AcademicClassDto>>> GetAcademicClassById(Guid id, CancellationToken cancellationToken)
        {
            var response = await _academicClassService.GetAcademicClassByIdAsync(id, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<CommonResponse<AcademicClassDto>>> UpdateAcademicClass(Guid id, [FromBody] UpdateAcademicClassCommand command, CancellationToken cancellationToken)
        {
            var response = await _academicClassService.UpdateAcademicClassAsync(id, command, cancellationToken);
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
        public async Task<ActionResult<CommonResponse<bool>>> DeleteAcademicClass(Guid id, CancellationToken cancellationToken)
        {
            var response = await _academicClassService.DeleteAcademicClassAsync(id, cancellationToken);
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

        [HttpPost("{id:guid}/sections")]
        public async Task<ActionResult<CommonResponse<ClassSectionDto>>> AddSection(Guid id, [FromBody] CreateClassSectionCommand command, CancellationToken cancellationToken)
        {
            var response = await _academicClassService.AddSectionAsync(id, command, cancellationToken);
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

        [HttpGet("{id:guid}/sections")]
        public async Task<ActionResult<CommonResponse<List<ClassSectionDto>>>> GetSections(Guid id, CancellationToken cancellationToken)
        {
            var response = await _academicClassService.GetSectionsAsync(id, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpPut("{id:guid}/sections/{classSectionId:guid}")]
        public async Task<ActionResult<CommonResponse<ClassSectionDto>>> UpdateSection(Guid id, Guid classSectionId, [FromBody] UpdateClassSectionCommand command, CancellationToken cancellationToken)
        {
            var response = await _academicClassService.UpdateSectionAsync(id, classSectionId, command, cancellationToken);
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

        [HttpDelete("{id:guid}/sections/{classSectionId:guid}")]
        public async Task<ActionResult<CommonResponse<bool>>> RemoveSection(Guid id, Guid classSectionId, CancellationToken cancellationToken)
        {
            var response = await _academicClassService.RemoveSectionAsync(id, classSectionId, cancellationToken);
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

        [HttpPost("{id:guid}/subjects")]
        public async Task<ActionResult<CommonResponse<ClassSubjectDto>>> AssignSubject(Guid id, [FromBody] AssignClassSubjectCommand command, CancellationToken cancellationToken)
        {
            var response = await _academicClassService.AssignSubjectAsync(id, command, cancellationToken);
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

        [HttpDelete("{id:guid}/subjects/{classSubjectId:guid}")]
        public async Task<ActionResult<CommonResponse<bool>>> RemoveSubject(Guid id, Guid classSubjectId, CancellationToken cancellationToken)
        {
            var response = await _academicClassService.RemoveSubjectAsync(id, classSubjectId, cancellationToken);
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

        [HttpGet("{id:guid}/subjects")]
        public async Task<ActionResult<CommonResponse<List<ClassSubjectDto>>>> GetClassSubjects(Guid id, [FromQuery] Guid? classSectionId, CancellationToken cancellationToken)
        {
            var response = await _academicClassService.GetClassSubjectsAsync(id, classSectionId, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }
    }
}
