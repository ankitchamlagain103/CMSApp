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

        [HttpPost("{id:guid}/discounts")]
        public async Task<ActionResult<CommonResponse<StudentDiscountDto>>> AddDiscount(Guid id, [FromBody] AddDiscountCommand command, CancellationToken cancellationToken)
        {
            var response = await _enrollmentService.AddDiscountAsync(id, command, cancellationToken);
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

        [HttpDelete("{id:guid}/discounts/{discountId:guid}")]
        public async Task<ActionResult<CommonResponse<bool>>> RemoveDiscount(Guid id, Guid discountId, CancellationToken cancellationToken)
        {
            var response = await _enrollmentService.RemoveDiscountAsync(id, discountId, cancellationToken);
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

        [HttpGet("{id:guid}/discounts")]
        public async Task<ActionResult<CommonResponse<List<StudentDiscountDto>>>> GetDiscounts(Guid id, CancellationToken cancellationToken)
        {
            var response = await _enrollmentService.GetDiscountsAsync(id, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpGet("discounts/summary")]
        public async Task<ActionResult<CommonResponse<List<AwardSummaryDto>>>> GetDiscountSummary([FromQuery] Guid? academicYearId, [FromQuery] string discountTypeCode, CancellationToken cancellationToken)
        {
            var response = await _enrollmentService.GetDiscountSummaryAsync(academicYearId, discountTypeCode, cancellationToken);
            return Ok(response);
        }

        [HttpPost("{id:guid}/scholarships")]
        public async Task<ActionResult<CommonResponse<StudentScholarshipDto>>> AddScholarship(Guid id, [FromBody] AddScholarshipCommand command, CancellationToken cancellationToken)
        {
            var response = await _enrollmentService.AddScholarshipAsync(id, command, cancellationToken);
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

        [HttpDelete("{id:guid}/scholarships/{scholarshipId:guid}")]
        public async Task<ActionResult<CommonResponse<bool>>> RemoveScholarship(Guid id, Guid scholarshipId, CancellationToken cancellationToken)
        {
            var response = await _enrollmentService.RemoveScholarshipAsync(id, scholarshipId, cancellationToken);
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

        [HttpGet("{id:guid}/scholarships")]
        public async Task<ActionResult<CommonResponse<List<StudentScholarshipDto>>>> GetScholarships(Guid id, CancellationToken cancellationToken)
        {
            var response = await _enrollmentService.GetScholarshipsAsync(id, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        // Cross-enrollment reporting -- "how many students got a scholarship" -- so it's a
        // top-level action rather than nested under one enrollment's {id}.
        [HttpGet("scholarships/summary")]
        public async Task<ActionResult<CommonResponse<List<AwardSummaryDto>>>> GetScholarshipSummary([FromQuery] Guid? academicYearId, [FromQuery] string scholarshipTypeCode, CancellationToken cancellationToken)
        {
            var response = await _enrollmentService.GetScholarshipSummaryAsync(academicYearId, scholarshipTypeCode, cancellationToken);
            return Ok(response);
        }

        [HttpPost("{id:guid}/fee-selections/{feeStructureItemId:guid}")]
        public async Task<ActionResult<CommonResponse<EnrollmentFeeSelectionDto>>> AddFeeSelection(Guid id, Guid feeStructureItemId, CancellationToken cancellationToken)
        {
            var response = await _enrollmentService.AddFeeSelectionAsync(id, feeStructureItemId, cancellationToken);
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

        [HttpDelete("{id:guid}/fee-selections/{feeSelectionId:guid}")]
        public async Task<ActionResult<CommonResponse<bool>>> RemoveFeeSelection(Guid id, Guid feeSelectionId, CancellationToken cancellationToken)
        {
            var response = await _enrollmentService.RemoveFeeSelectionAsync(id, feeSelectionId, cancellationToken);
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

        [HttpGet("{id:guid}/fee-selections")]
        public async Task<ActionResult<CommonResponse<List<EnrollmentFeeSelectionDto>>>> GetFeeSelections(Guid id, CancellationToken cancellationToken)
        {
            var response = await _enrollmentService.GetFeeSelectionsAsync(id, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpGet("{id:guid}/fee-structure")]
        public async Task<ActionResult<CommonResponse<EnrollmentFeeStructureDto>>> GetFeeStructure(Guid id, CancellationToken cancellationToken)
        {
            var response = await _enrollmentService.GetFeeStructureAsync(id, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpGet("{id:guid}/fee-receipt-preview")]
        public async Task<ActionResult<CommonResponse<DocumentPreviewDto>>> GetFeeReceiptPreview(Guid id, CancellationToken cancellationToken)
        {
            var response = await _enrollmentService.GetFeeReceiptPreviewAsync(id, cancellationToken);
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
