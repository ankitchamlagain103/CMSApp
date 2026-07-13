using Application.Common.Models;
using Application.Students;
using Application.Students.Commands;
using Application.Students.Dtos;
using Application.Students.Queries;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StudentsController : ControllerBase
    {
        private readonly IStudentService _studentService;

        public StudentsController(IStudentService studentService)
        {
            _studentService = studentService;
        }

        [HttpPost]
        public async Task<ActionResult<CommonResponse<StudentDto>>> CreateStudent([FromBody] CreateStudentCommand command, CancellationToken cancellationToken)
        {
            var response = await _studentService.CreateStudentAsync(command, cancellationToken);
            if (response.ResponseCode != ResponseCodes.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpGet]
        public async Task<ActionResult<CommonResponse<PaginatedResponse<StudentDto>>>> GetStudents([FromQuery] GetStudentsQuery query, CancellationToken cancellationToken)
        {
            var response = await _studentService.GetStudentsAsync(query, cancellationToken);
            return Ok(response);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<CommonResponse<StudentDto>>> GetStudentById(Guid id, CancellationToken cancellationToken)
        {
            var response = await _studentService.GetStudentByIdAsync(id, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<CommonResponse<StudentDto>>> UpdateStudent(Guid id, [FromBody] UpdateStudentCommand command, CancellationToken cancellationToken)
        {
            var response = await _studentService.UpdateStudentAsync(id, command, cancellationToken);
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
        public async Task<ActionResult<CommonResponse<bool>>> DeleteStudent(Guid id, CancellationToken cancellationToken)
        {
            var response = await _studentService.DeleteStudentAsync(id, cancellationToken);
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

        [HttpPost("{id:guid}/guardians")]
        public async Task<ActionResult<CommonResponse<StudentGuardianDto>>> LinkGuardian(Guid id, [FromBody] LinkGuardianCommand command, CancellationToken cancellationToken)
        {
            var response = await _studentService.LinkGuardianAsync(id, command, cancellationToken);
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

        [HttpDelete("{id:guid}/guardians/{linkId:guid}")]
        public async Task<ActionResult<CommonResponse<bool>>> UnlinkGuardian(Guid id, Guid linkId, CancellationToken cancellationToken)
        {
            var response = await _studentService.UnlinkGuardianAsync(id, linkId, cancellationToken);
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

        [HttpGet("{id:guid}/guardians")]
        public async Task<ActionResult<CommonResponse<List<StudentGuardianDto>>>> GetGuardians(Guid id, CancellationToken cancellationToken)
        {
            var response = await _studentService.GetGuardiansAsync(id, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }
    }
}
