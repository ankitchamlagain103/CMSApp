using Domain.Enums;

namespace Application.Common.Models
{
    // Shared response shape for every "preview" endpoint (payslip, fee receipt, ID cards) --
    // Html is the fully token-substituted document, ready for the frontend to display/print.
    public class DocumentPreviewDto
    {
        public DocumentTemplateType TemplateType { get; set; }
        public string Html { get; set; }
    }
}
