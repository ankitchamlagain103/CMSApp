using Domain.Enums;

namespace Application.Dashboard.Dtos
{
    public class FeeInvoiceStatusCountDto
    {
        public FeeInvoiceStatus Status { get; set; }
        public int Count { get; set; }
    }
}
