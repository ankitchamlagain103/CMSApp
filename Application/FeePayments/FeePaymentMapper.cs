using Application.FeePayments.Dtos;
using Domain.Entities;

namespace Application.FeePayments
{
    public static class FeePaymentMapper
    {
        public static FeePaymentDto ToDto(FeePayment payment)
        {
            var paymentDto = new FeePaymentDto
            {
                Id = payment.Id,
                ReceiptNo = payment.ReceiptNo,
                EnrollmentId = payment.EnrollmentId,
                PaymentDate = payment.PaymentDate,
                Amount = payment.Amount,
                PaymentMode = payment.PaymentMode,
                ReferenceNo = payment.ReferenceNo,
                Status = payment.Status,
                Remarks = payment.Remarks
            };

            var student = payment.Enrollment?.Student;
            if (student != null)
            {
                var nameParts = new List<string>();
                if (!string.IsNullOrWhiteSpace(student.FirstName))
                {
                    nameParts.Add(student.FirstName);
                }

                if (!string.IsNullOrWhiteSpace(student.MiddleName))
                {
                    nameParts.Add(student.MiddleName);
                }

                if (!string.IsNullOrWhiteSpace(student.LastName))
                {
                    nameParts.Add(student.LastName);
                }

                paymentDto.StudentName = string.Join(" ", nameParts);
            }

            foreach (var allocation in payment.Allocations)
            {
                var allocationDto = new FeePaymentAllocationDto
                {
                    FeeInvoiceId = allocation.FeeInvoiceId,
                    Amount = allocation.Amount
                };

                if (allocation.FeeInvoice != null)
                {
                    allocationDto.InvoiceNo = allocation.FeeInvoice.InvoiceNo;
                    allocationDto.BillingYear = allocation.FeeInvoice.BillingYear;
                    allocationDto.BillingMonth = allocation.FeeInvoice.BillingMonth;
                    allocationDto.SettlesInvoice = allocation.FeeInvoice.PaidAmount >= allocation.FeeInvoice.NetAmount;
                }

                paymentDto.Allocations.Add(allocationDto);
            }

            return paymentDto;
        }
    }
}
