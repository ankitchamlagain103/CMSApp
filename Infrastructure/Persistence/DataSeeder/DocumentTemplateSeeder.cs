using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Persistence.DataSeeder
{
    // Seeds one default HTML template per DocumentTemplateType (see
    // Docs/document_preview_implementation_guide.md for the full placeholder-token catalog each
    // template can use). Idempotent by TemplateType and strictly create-if-missing: an existing
    // row is NEVER updated, so an admin's hand-edited template survives every restart.
    public static class DocumentTemplateSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var baselineTemplates = BuildBaselineTemplates();

            var existingTypes = await dbContext.DocumentTemplates
                .Select(template => template.TemplateType)
                .ToListAsync();

            var newRowsAdded = false;
            foreach (var baselineTemplate in baselineTemplates)
            {
                if (existingTypes.Contains(baselineTemplate.TemplateType))
                {
                    continue;
                }

                dbContext.DocumentTemplates.Add(baselineTemplate);
                newRowsAdded = true;
            }

            if (newRowsAdded)
            {
                await dbContext.SaveChangesAsync();
            }
        }

        private static List<DocumentTemplate> BuildBaselineTemplates()
        {
            var baselineTemplates = new List<DocumentTemplate>
            {
                BuildTemplate(DocumentTemplateType.Payslip, "Default Payslip", BuildPayslipHtml()),
                BuildTemplate(DocumentTemplateType.FeeReceipt, "Default Fee Receipt", BuildFeeReceiptHtml()),
                BuildTemplate(DocumentTemplateType.StudentIdCard, "Default Student ID Card", BuildStudentIdCardHtml()),
                BuildTemplate(DocumentTemplateType.TeacherIdCard, "Default Teacher ID Card", BuildTeacherIdCardHtml()),
                BuildTemplate(DocumentTemplateType.PaymentReceipt, "Default Payment Receipt", BuildPaymentReceiptHtml())
            };

            return baselineTemplates;
        }

        private static DocumentTemplate BuildTemplate(DocumentTemplateType templateType, string name, string htmlContent)
        {
            var documentTemplate = new DocumentTemplate
            {
                TemplateType = templateType,
                Name = name,
                HtmlContent = htmlContent
            };

            return documentTemplate;
        }

        private static string BuildPayslipHtml()
        {
            return "<div style=\"font-family:Arial,sans-serif;max-width:700px;margin:auto;padding:16px;border:1px solid #ccc;\">"
                + "<h2>Payslip</h2>"
                + "<p><strong>{{EmployeeName}}</strong> ({{EmployeeCode}}) - {{JobPositionCode}}</p>"
                + "<p>Effective From: {{EffectiveFromDate}} | Fiscal Year: {{FiscalYearCode}}</p>"
                + "<h3>Earnings</h3>"
                + "<table style=\"width:100%;border-collapse:collapse;\"><thead><tr><th>Component</th><th>Type</th><th>Amount</th><th>Frequency</th></tr></thead><tbody>{{ComponentsRows}}</tbody></table>"
                + "<h3>Deductions</h3>"
                + "<table style=\"width:100%;border-collapse:collapse;\"><thead><tr><th>Deduction</th><th>Type</th><th>Amount</th><th>Frequency</th></tr></thead><tbody>{{DeductionsRows}}</tbody></table>"
                + "<h3>Insurance Premiums</h3>"
                + "<table style=\"width:100%;border-collapse:collapse;\"><thead><tr><th>Type</th><th>Annual Premium</th></tr></thead><tbody>{{InsurancePremiumsRows}}</tbody></table>"
                + "<h3>Investment and Tax Planning</h3>"
                + "<p>Gross Annual Income: {{GrossAnnualIncome}}</p>"
                + "<p>Retirement Contribution (Annual): {{RetirementContributionAnnual}}</p>"
                + "<p>Retirement Exemption: {{RetirementExemption}}</p>"
                + "<p>Insurance Deduction: {{InsuranceDeduction}}</p>"
                + "<p>Annual Taxable Income: {{AnnualTaxableIncome}}</p>"
                + "<table style=\"width:100%;border-collapse:collapse;\"><thead><tr><th>Min</th><th>Max</th><th>Rate</th><th>Taxable Amount</th><th>Tax</th></tr></thead><tbody>{{TaxBreakdownRows}}</tbody></table>"
                + "<p>Annual Tax: {{AnnualTax}} | Monthly Tax: {{MonthlyTax}}</p>"
                + "<h3>Summary</h3>"
                + "<p>Gross Monthly: {{GrossMonthly}}</p>"
                + "<p><strong>Net Monthly: {{NetMonthly}}</strong></p>"
                + "</div>";
        }

        private static string BuildFeeReceiptHtml()
        {
            return "<div style=\"font-family:Arial,sans-serif;max-width:700px;margin:auto;padding:16px;border:1px solid #ccc;\">"
                + "<h2>Fee Receipt</h2>"
                + "<p><strong>{{StudentName}}</strong> ({{AdmissionNo}}) - Grade {{GradeCode}} {{SectionCode}}, Roll No {{RollNumber}}</p>"
                + "<h3>Fee Items</h3>"
                + "<table style=\"width:100%;border-collapse:collapse;\"><thead><tr><th>Category</th><th>Amount</th><th>Frequency</th><th>Applies</th></tr></thead><tbody>{{FeeItemsRows}}</tbody></table>"
                + "<h3>Discounts</h3>"
                + "<table style=\"width:100%;border-collapse:collapse;\"><thead><tr><th>Type</th><th>Value</th></tr></thead><tbody>{{DiscountsRows}}</tbody></table>"
                + "<h3>Scholarships</h3>"
                + "<table style=\"width:100%;border-collapse:collapse;\"><thead><tr><th>Type</th><th>Value</th></tr></thead><tbody>{{ScholarshipsRows}}</tbody></table>"
                + "<h3>Summary</h3>"
                + "<p>Monthly Recurring Total: {{MonthlyRecurringTotal}} (of which {{AnnualInstallmentMonthlyShare}} is the Annual Fee's installment share)</p>"
                + "<p>Annual Total: {{AnnualTotal}}</p>"
                + "<p>One-Time Total: {{OneTimeTotal}}</p>"
                + "<p>Refundable Deposit Total: {{RefundableDepositTotal}}</p>"
                + "<p>Total Discount Reduction: {{TotalDiscountReduction}}</p>"
                + "<p>Total Scholarship Reduction: {{TotalScholarshipReduction}}</p>"
                + "<p><strong>Net Monthly Recurring: {{NetMonthlyRecurring}}</strong></p>"
                + "</div>";
        }

        // Modeled on the school's own "Receipt Sample for Fees" layout: a bordered card with a
        // school-header band, a Sr.No/Particulars/Amount table (fed by every allocated
        // invoice's own lines -- the "receipt should include invoice line details"
        // requirement), a Total row, Paid-By/Balance line, and signature blocks. Deliberately
        // does NOT carry the sample's "non-refundable" disclaimer -- this system tracks
        // genuinely refundable deposit items (FeeStructureItem.IsRefundable), so that claim
        // would be factually wrong here; the footer stays neutral instead.
        private static string BuildPaymentReceiptHtml()
        {
            return "<div style=\"font-family:Georgia,'Times New Roman',serif;max-width:700px;margin:auto;border:2px solid #333;\">"
                + "<div style=\"background:#fff8dc;padding:16px;text-align:center;border-bottom:2px solid #333;\">"
                + "<div style=\"text-decoration:underline;font-weight:bold;\">Receipt</div>"
                + "<h2 style=\"color:#b03a2e;margin:6px 0;\">{{SchoolName}}</h2>"
                + "<table style=\"width:100%;font-size:14px;\"><tr>"
                + "<td style=\"text-align:left;\">Address: {{SchoolAddress}}</td>"
                + "<td style=\"text-align:right;\">Phone: {{SchoolPhone}}</td>"
                + "</tr></table>"
                + "</div>"
                + "<div style=\"padding:16px;background:#fff8dc;\">"
                + "<p><strong>Receipt No.</strong> {{ReceiptNo}}</p>"
                + "<table style=\"width:100%;font-size:14px;\"><tr>"
                + "<td>Name of Student: <strong>{{StudentName}}</strong> ({{AdmissionNo}})</td>"
                + "<td>Grade/Section: {{GradeCode}} {{SectionCode}}</td>"
                + "</tr><tr>"
                + "<td colspan=\"2\">Date of Payment: {{PaymentDate}}</td>"
                + "</tr></table>"
                + "</div>"
                + "<table style=\"width:100%;border-collapse:collapse;border-top:2px solid #333;\">"
                + "<thead><tr style=\"background:#fff8dc;\">"
                + "<th style=\"border:1px solid #333;padding:6px;\">Sr. No</th>"
                + "<th style=\"border:1px solid #333;padding:6px;\">Invoice No</th>"
                + "<th style=\"border:1px solid #333;padding:6px;text-align:left;\">Particulars</th>"
                + "<th style=\"border:1px solid #333;padding:6px;\">Amount</th>"
                + "</tr></thead>"
                + "<tbody>{{InvoiceLinesRows}}</tbody>"
                + "<tfoot><tr style=\"background:#fff8dc;font-weight:bold;\">"
                + "<td colspan=\"3\" style=\"border:1px solid #333;padding:6px;text-align:right;\">Total</td>"
                + "<td style=\"border:1px solid #333;padding:6px;text-align:center;\">{{AmountPaid}}</td>"
                + "</tr></tfoot>"
                + "</table>"
                + "<h4 style=\"padding:0 16px;\">Invoices Settled</h4>"
                + "<table style=\"width:100%;border-collapse:collapse;padding:0 16px;\">"
                + "<thead><tr><th>Invoice No</th><th>Billing Month</th><th>Allocated</th></tr></thead>"
                + "<tbody>{{AllocationsRows}}</tbody>"
                + "</table>"
                + "<div style=\"padding:16px;background:#fff8dc;border-top:2px solid #333;\">"
                + "<table style=\"width:100%;font-size:14px;\"><tr>"
                + "<td>Paid By: {{PaymentMode}} {{ReferenceNo}}</td>"
                + "<td style=\"text-align:right;\">Balance if any: {{OutstandingAmount}}</td>"
                + "</tr></table>"
                + "<p>Remarks: {{Remarks}}</p>"
                + "</div>"
                + "<div style=\"padding:24px 16px 16px;display:flex;justify-content:space-between;\">"
                + "<div>Signature of Cashier</div>"
                + "<div>Signature of Guardian</div>"
                + "</div>"
                + "<div style=\"border-top:2px solid #333;background:#fff8dc;padding:8px;text-align:center;font-size:12px;\">"
                + "This is a computer-generated receipt."
                + "</div>"
                + "</div>";
        }

        private static string BuildStudentIdCardHtml()
        {
            return "<div style=\"font-family:Arial,sans-serif;width:340px;padding:12px;border:1px solid #333;border-radius:8px;\">"
                + "<h3>Student ID Card</h3>"
                + "<p><strong>{{StudentName}}</strong></p>"
                + "<p>Admission No: {{AdmissionNo}}</p>"
                + "<p>Grade {{GradeCode}} {{SectionCode}} | Roll No {{RollNumber}}</p>"
                + "<p>Date of Birth: {{DateOfBirth}}</p>"
                + "<p>Guardian: {{GuardianName}} ({{GuardianPhone}})</p>"
                + "</div>";
        }

        private static string BuildTeacherIdCardHtml()
        {
            return "<div style=\"font-family:Arial,sans-serif;width:340px;padding:12px;border:1px solid #333;border-radius:8px;\">"
                + "<h3>Teacher ID Card</h3>"
                + "<p><strong>{{TeacherName}}</strong></p>"
                + "<p>Employee Code: {{EmployeeCode}}</p>"
                + "<p>Position: {{JobPositionCode}}</p>"
                + "<p>License No: {{TeachingLicenseNo}} | Specialization: {{Specialization}}</p>"
                + "<p>Joined: {{JoinDate}}</p>"
                + "<p>{{Phone}} | {{Email}}</p>"
                + "</div>";
        }
    }
}
