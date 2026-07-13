using Application.Common.Models;
using Application.Enrollments;
using Application.Enrollments.Commands;
using Application.Enrollments.Dtos;
using Application.Enrollments.Queries;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EnrollmentsController : ControllerBase
    {
        private readonly IEnrollmentService _enrollmentService;

        public EnrollmentsController(IEnrollmentService enrollmentService)
        {
            _enrollmentService = enrollmentService;
        }

        [HttpPost]
        public async Task<ActionResult<CommonResponse<EnrollmentDto>>> CreateEnrollment([FromBody] CreateEnrollmentCommand command, CancellationToken cancellationToken)
        {
            var response = await _enrollmentService.CreateEnrollmentAsync(command, cancellationToken);
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
        public async Task<ActionResult<CommonResponse<PaginatedResponse<EnrollmentDto>>>> GetEnrollments([FromQuery] GetEnrollmentsQuery query, CancellationToken cancellationToken)
        {
            var response = await _enrollmentService.GetEnrollmentsAsync(query, cancellationToken);
            return Ok(response);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<CommonResponse<EnrollmentDto>>> GetEnrollmentById(Guid id, CancellationToken cancellationToken)
        {
            var response = await _enrollmentService.GetEnrollmentByIdAsync(id, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<CommonResponse<EnrollmentDto>>> UpdateEnrollment(Guid id, [FromBody] UpdateEnrollmentCommand command, CancellationToken cancellationToken)
        {
            var response = await _enrollmentService.UpdateEnrollmentAsync(id, command, cancellationToken);
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
        public async Task<ActionResult<CommonResponse<bool>>> DeleteEnrollment(Guid id, CancellationToken cancellationToken)
        {
            var response = await _enrollmentService.DeleteEnrollmentAsync(id, cancellationToken);
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

        [HttpPost("{id:guid}/subjects/{classSubjectId:guid}")]
        public async Task<ActionResult<CommonResponse<EnrollmentSubjectDto>>> AddElectiveSubject(Guid id, Guid classSubjectId, CancellationToken cancellationToken)
        {
            var response = await _enrollmentService.AddElectiveSubjectAsync(id, classSubjectId, cancellationToken);
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

        [HttpDelete("{id:guid}/subjects/{electiveSubjectId:guid}")]
        public async Task<ActionResult<CommonResponse<bool>>> RemoveElectiveSubject(Guid id, Guid electiveSubjectId, CancellationToken cancellationToken)
        {
            var response = await _enrollmentService.RemoveElectiveSubjectAsync(id, electiveSubjectId, cancellationToken);
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
        public async Task<ActionResult<CommonResponse<List<EnrollmentSubjectDto>>>> GetElectiveSubjects(Guid id, CancellationToken cancellationToken)
        {
            var response = await _enrollmentService.GetElectiveSubjectsAsync(id, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }
    }
}
