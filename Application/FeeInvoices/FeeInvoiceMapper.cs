using Application.Common.Helpers;
using Application.FeeInvoices.Dtos;
using Domain.Entities;

namespace Application.FeeInvoices
{
    public static class FeeInvoiceMapper
    {
        // labelsByCode (2026-07-19): merged Config label map (fee categories + adjustment
        // types); when supplied, the DTOs carry human-readable labels alongside the raw codes.
        // Null keeps the label fields at the code itself, so no consumer ever sees a blank.
        public static FeeInvoiceDto ToDto(FeeInvoice invoice, bool includeLines, IReadOnlyDictionary<string, string> labelsByCode = null)
        {
            var invoiceDto = new FeeInvoiceDto
            {
                Id = invoice.Id,
                InvoiceNo = invoice.InvoiceNo,
                EnrollmentId = invoice.EnrollmentId,
                AcademicYearId = invoice.AcademicYearId,
                BillingYear = invoice.BillingYear,
                BillingMonth = invoice.BillingMonth,
                Status = invoice.Status,
                GrossAmount = invoice.GrossAmount,
                DiscountAmount = invoice.DiscountAmount,
                NetAmount = invoice.NetAmount,
                PaidAmount = invoice.PaidAmount,
                BalanceAmount = invoice.NetAmount - invoice.PaidAmount,
                PreviousDueAmount = invoice.PreviousDueAmount,
                CarriedForwardAmount = invoice.CarriedForwardAmount,
                CarriedForwardToInvoiceId = invoice.CarriedForwardToInvoiceId,
                DueDate = invoice.DueDate,
                GeneratedTs = invoice.GeneratedTs,
                Remarks = invoice.Remarks
            };

            var enrollment = invoice.Enrollment;
            if (enrollment != null)
            {
                if (enrollment.Student != null)
                {
                    invoiceDto.StudentId = enrollment.Student.Id;
                    invoiceDto.StudentName = BuildFullName(enrollment.Student.FirstName, enrollment.Student.MiddleName, enrollment.Student.LastName);
                    invoiceDto.AdmissionNo = enrollment.Student.AdmissionNo;
                }

                if (enrollment.ClassSection != null)
                {
                    invoiceDto.SectionCode = enrollment.ClassSection.SectionCode;
                    invoiceDto.GradeCode = enrollment.ClassSection.AcademicClass?.GradeCode;
                }
            }

            if (includeLines)
            {
                foreach (var line in invoice.Lines)
                {
                    var lineDto = ToLineDto(line, labelsByCode);
                    invoiceDto.Lines.Add(lineDto);
                }
            }

            return invoiceDto;
        }

        public static FeeInvoiceLineDto ToLineDto(FeeInvoiceLine line, IReadOnlyDictionary<string, string> labelsByCode = null)
        {
            var lineDto = new FeeInvoiceLineDto
            {
                Id = line.Id,
                Source = line.Source,
                FeeCategoryCode = line.FeeCategoryCode,
                FeeCategoryLabel = ConfigLabelHelper.Resolve(labelsByCode, line.FeeCategoryCode),
                Description = line.Description,
                Amount = line.Amount
            };

            return lineDto;
        }

        public static FeeAdjustmentDto ToAdjustmentDto(FeeAdjustment adjustment, IReadOnlyDictionary<string, string> labelsByCode = null)
        {
            var adjustmentDto = new FeeAdjustmentDto
            {
                Id = adjustment.Id,
                EnrollmentId = adjustment.EnrollmentId,
                BillingYear = adjustment.BillingYear,
                BillingMonth = adjustment.BillingMonth,
                AdjustmentTypeCode = adjustment.AdjustmentTypeCode,
                AdjustmentTypeLabel = ConfigLabelHelper.Resolve(labelsByCode, adjustment.AdjustmentTypeCode),
                FeeCategoryCode = adjustment.FeeCategoryCode,
                FeeCategoryLabel = ConfigLabelHelper.Resolve(labelsByCode, adjustment.FeeCategoryCode),
                Direction = adjustment.Direction,
                ValueType = adjustment.ValueType,
                Value = adjustment.Value,
                Remarks = adjustment.Remarks,
                Status = adjustment.Status,
                AppliedFeeInvoiceId = adjustment.AppliedFeeInvoiceId
            };

            var enrollment = adjustment.Enrollment;
            if (enrollment != null)
            {
                if (enrollment.Student != null)
                {
                    adjustmentDto.StudentName = BuildFullName(enrollment.Student.FirstName, enrollment.Student.MiddleName, enrollment.Student.LastName);
                    adjustmentDto.AdmissionNo = enrollment.Student.AdmissionNo;
                }

                if (enrollment.ClassSection != null)
                {
                    adjustmentDto.SectionCode = enrollment.ClassSection.SectionCode;
                    adjustmentDto.GradeCode = enrollment.ClassSection.AcademicClass?.GradeCode;
                }
            }

            return adjustmentDto;
        }

        private static string BuildFullName(string firstName, string middleName, string lastName)
        {
            var nameParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(firstName))
            {
                nameParts.Add(firstName);
            }

            if (!string.IsNullOrWhiteSpace(middleName))
            {
                nameParts.Add(middleName);
            }

            if (!string.IsNullOrWhiteSpace(lastName))
            {
                nameParts.Add(lastName);
            }

            var fullName = string.Join(" ", nameParts);
            return fullName;
        }
    }
}
