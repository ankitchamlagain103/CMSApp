using Application.Common.Models;
using Application.DocumentTemplates;
using Application.DocumentTemplates.Commands;
using Application.DocumentTemplates.Dtos;
using Application.DocumentTemplates.Queries;
using Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentTemplatesController : ControllerBase
    {
        private readonly IDocumentTemplateService _documentTemplateService;

        public DocumentTemplatesController(IDocumentTemplateService documentTemplateService)
        {
            _documentTemplateService = documentTemplateService;
        }

        [HttpPost]
        public async Task<ActionResult<CommonResponse<DocumentTemplateDto>>> CreateDocumentTemplate([FromBody] CreateDocumentTemplateCommand command, CancellationToken cancellationToken)
        {
            var response = await _documentTemplateService.CreateDocumentTemplateAsync(command, cancellationToken);
            if (response.ResponseCode != ResponseCodes.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpGet]
        public async Task<ActionResult<CommonResponse<PaginatedResponse<DocumentTemplateDto>>>> GetDocumentTemplates([FromQuery] GetDocumentTemplatesQuery query, CancellationToken cancellationToken)
        {
            var response = await _documentTemplateService.GetDocumentTemplatesAsync(query, cancellationToken);
            return Ok(response);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<CommonResponse<DocumentTemplateDto>>> GetDocumentTemplateById(Guid id, CancellationToken cancellationToken)
        {
            var response = await _documentTemplateService.GetDocumentTemplateByIdAsync(id, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<CommonResponse<DocumentTemplateDto>>> UpdateDocumentTemplate(Guid id, [FromBody] UpdateDocumentTemplateCommand command, CancellationToken cancellationToken)
        {
            var response = await _documentTemplateService.UpdateDocumentTemplateAsync(id, command, cancellationToken);
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
        public async Task<ActionResult<CommonResponse<bool>>> DeleteDocumentTemplate(Guid id, CancellationToken cancellationToken)
        {
            var response = await _documentTemplateService.DeleteDocumentTemplateAsync(id, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpGet("placeholders/{templateType}")]
        public async Task<ActionResult<CommonResponse<List<TemplatePlaceholderDto>>>> GetPlaceholders(DocumentTemplateType templateType, CancellationToken cancellationToken)
        {
            var response = await _documentTemplateService.GetPlaceholdersAsync(templateType, cancellationToken);
            return Ok(response);
        }
    }
}
