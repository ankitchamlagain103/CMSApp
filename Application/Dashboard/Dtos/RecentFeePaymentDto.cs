namespace Application.Dashboard.Dtos
{
    public class RecentFeePaymentDto
    {
        public Guid Id { get; set; }
        public string ReceiptNo { get; set; }
        public string StudentName { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
    }
}
