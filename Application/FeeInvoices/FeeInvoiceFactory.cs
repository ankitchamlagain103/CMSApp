using Application.Common.Helpers;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;

namespace Application.FeeInvoices
{
    // The one place a monthly fee invoice is assembled from configuration (structure items,
    // discounts/scholarships, opt-in selections, pending adjustments). Shared by the bulk
    // generation run (FeeInvoiceService.GenerateAsync) and advance payment
    // (FeePaymentService billing future months at collection time, 2026-07-17) so the two can
    // never disagree on line composition. Pure static, same convention as TaxCalculator /
    // FeeRuleEngine.
    public static class FeeInvoiceFactory
    {
        // Shared by FeeInvoiceService (bulk generation) and FeePaymentService (advance-payment
        // billing, 2026-07-17) so invoice numbers never collide across the two paths.
        public const string InvoiceNoPrefix = "INV";

        // stampAdjustments: the generation/confirm paths stamp each folded-in adjustment
        // Applied; payment PREVIEW passes false so tracked FeeAdjustment entities are never
        // mutated by a read-only call.
        // labelsByCode (2026-07-19): merged Config label map (fee categories + discount/
        // scholarship/adjustment types) -- line Descriptions are written with the human-readable
        // label ("Tuition Fee (Monthly)") instead of the raw code; null falls back to codes.
        public static FeeInvoice BuildInvoice(
            Guid invoiceId,
            Enrollment enrollment,
            AcademicYear academicYear,
            FeeStructure feeStructure,
            IReadOnlyList<FeeInvoice> earlierInvoices,
            IReadOnlyList<StudentDiscount> discounts,
            IReadOnlyList<StudentScholarship> scholarships,
            IReadOnlyList<Guid> selectedItemIds,
            IReadOnlyList<FeeAdjustment> pendingAdjustments,
            int billingYear,
            int billingMonth,
            DateTime dueDate,
            string invoiceNo,
            bool stampAdjustments,
            IReadOnlyDictionary<string, string> labelsByCode = null)
        {
            var invoice = new FeeInvoice
            {
                Id = invoiceId,
                InvoiceNo = invoiceNo,
                EnrollmentId = enrollment.Id,
                AcademicYearId = academicYear.Id,
                BillingYear = billingYear,
                BillingMonth = billingMonth,
                Status = FeeInvoiceStatus.Draft,
                DueDate = dueDate,
                Enrollment = enrollment,
                AcademicYear = academicYear
            };

            var hasEarlierInvoice = earlierInvoices.Count > 0;
            decimal previousDue = 0m;

            // Restricted to invoices whose (BillingYear, BillingMonth) is strictly before the
            // period being built -- earlierInvoices is every non-cancelled invoice of the
            // enrollment regardless of period, which can include a later invoice (e.g. one
            // created ahead of time by advance billing). Without this filter, generating an
            // earlier month after a later one already exists would misstate PreviousDueAmount.
            foreach (var earlierInvoice in earlierInvoices)
            {
                if (earlierInvoice.Status != FeeInvoiceStatus.Draft
                    && IsBillingPeriodBefore(earlierInvoice.BillingYear, earlierInvoice.BillingMonth, billingYear, billingMonth))
                {
                    previousDue += earlierInvoice.NetAmount - earlierInvoice.PaidAmount;
                }
            }

            invoice.PreviousDueAmount = previousDue;

            foreach (var item in feeStructure.Items)
            {
                if (item.IsOptional && !selectedItemIds.Contains(item.Id))
                {
                    continue;
                }

                var categoryLabel = ConfigLabelHelper.Resolve(labelsByCode, item.FeeCategoryCode);

                if (item.FrequencyType == FeeFrequencyType.Monthly)
                {
                    var monthlyLine = new FeeInvoiceLine
                    {
                        FeeInvoiceId = invoice.Id,
                        Source = FeeLineSource.StructureItem,
                        FeeStructureItemId = item.Id,
                        FeeCategoryCode = item.FeeCategoryCode,
                        Description = categoryLabel + " (Monthly)",
                        Amount = item.Amount,
                        FeeInvoice = invoice
                    };
                    invoice.Lines.Add(monthlyLine);
                    continue;
                }

                if (item.FrequencyType == FeeFrequencyType.Annual)
                {
                    var installmentCount = item.InstallmentCount ?? 1;

                    // Remaining-balance-driven (2026-07-17), not installment-index-driven: sum
                    // what's already been billed for this item across every earlier invoice
                    // (Draft included, same convention as hasEarlierInvoice) and bill the
                    // remainder over whatever installments are left. When nothing unusual has
                    // happened this reproduces the exact same numbers as a fixed
                    // item.Amount/installmentCount schedule (verified: each step's "amount
                    // already billed" is itself an even share, so "remaining / installments
                    // left" telescopes to the same per-installment amount, and the final
                    // installment still absorbs all accumulated rounding). The payoff: an admin
                    // can settle the item in full on one Draft invoice (POST
                    // .../settle-annual-in-full, or just editing the line amount up) and every
                    // later month automatically sees remainingForItem <= 0 and stops billing it
                    // -- no separate "already fully paid" flag/table needed.
                    decimal alreadyBilledForItem = 0m;
                    var installmentsSoFar = 0;
                    foreach (var earlierInvoice in earlierInvoices)
                    {
                        if (!IsBillingPeriodBefore(earlierInvoice.BillingYear, earlierInvoice.BillingMonth, billingYear, billingMonth))
                        {
                            continue;
                        }

                        foreach (var earlierLine in earlierInvoice.Lines)
                        {
                            if (earlierLine.Source == FeeLineSource.AnnualInstallment && earlierLine.FeeStructureItemId == item.Id)
                            {
                                alreadyBilledForItem += earlierLine.Amount;
                                installmentsSoFar++;
                            }
                        }
                    }

                    var remainingForItem = item.Amount - alreadyBilledForItem;
                    if (remainingForItem <= 0m || installmentsSoFar >= installmentCount)
                    {
                        continue;
                    }

                    if (installmentCount <= 1)
                    {
                        var fullAnnualLine = new FeeInvoiceLine
                        {
                            FeeInvoiceId = invoice.Id,
                            Source = FeeLineSource.AnnualInstallment,
                            FeeStructureItemId = item.Id,
                            FeeCategoryCode = item.FeeCategoryCode,
                            Description = categoryLabel + " (Annual)",
                            Amount = remainingForItem,
                            FeeInvoice = invoice
                        };
                        invoice.Lines.Add(fullAnnualLine);
                        continue;
                    }

                    var installmentsLeft = installmentCount - installmentsSoFar;
                    var installmentAmount = installmentsLeft == 1
                        ? remainingForItem
                        : Math.Round(remainingForItem / installmentsLeft, 2);
                    var installmentIndexForLabel = installmentsSoFar + 1;

                    var installmentLine = new FeeInvoiceLine
                    {
                        FeeInvoiceId = invoice.Id,
                        Source = FeeLineSource.AnnualInstallment,
                        FeeStructureItemId = item.Id,
                        FeeCategoryCode = item.FeeCategoryCode,
                        Description = categoryLabel + " (Annual, installment " + installmentIndexForLabel + "/" + installmentCount + ")",
                        Amount = installmentAmount,
                        FeeInvoice = invoice
                    };
                    invoice.Lines.Add(installmentLine);
                    continue;
                }

                // OneTime: first invoice of the enrollment only (F2).
                if (hasEarlierInvoice)
                {
                    continue;
                }

                var oneTimeLine = new FeeInvoiceLine
                {
                    FeeInvoiceId = invoice.Id,
                    Source = FeeLineSource.OneTimeCharge,
                    FeeStructureItemId = item.Id,
                    FeeCategoryCode = item.FeeCategoryCode,
                    Description = categoryLabel + " (One time)" + (item.IsRefundable ? " (Refundable)" : string.Empty),
                    Amount = item.Amount,
                    FeeInvoice = invoice
                };
                invoice.Lines.Add(oneTimeLine);
            }

            // Recurring subtotal (Monthly + AnnualInstallment) is the base recurring awards and
            // percentage adjustments resolve against; OneTime charges are never discounted (F4).
            // Also broken down per FeeCategoryCode (2026-07-17) so a category-scoped
            // FeeAdjustment (the bulk-adjustment "Fee Category" field) resolves a Percentage
            // value against just that category's subtotal -- same convention as
            // FeeRuleEngine.ResolveBaseAmount for payment-time rules.
            decimal recurringSubtotal = 0m;
            var recurringSubtotalByCategory = new Dictionary<string, decimal>();
            foreach (var line in invoice.Lines)
            {
                if (line.Source == FeeLineSource.StructureItem || line.Source == FeeLineSource.AnnualInstallment)
                {
                    recurringSubtotal += line.Amount;

                    if (!string.IsNullOrWhiteSpace(line.FeeCategoryCode))
                    {
                        recurringSubtotalByCategory.TryGetValue(line.FeeCategoryCode, out var categoryAmount);
                        recurringSubtotalByCategory[line.FeeCategoryCode] = categoryAmount + line.Amount;
                    }
                }
            }

            foreach (var discount in discounts)
            {
                var discountAmount = ResolveAwardAmount(discount.ValueType, discount.Value, recurringSubtotal);
                if (discountAmount <= 0m)
                {
                    continue;
                }

                var discountLine = new FeeInvoiceLine
                {
                    FeeInvoiceId = invoice.Id,
                    Source = FeeLineSource.Discount,
                    StudentDiscountId = discount.Id,
                    Description = "Discount - " + ConfigLabelHelper.Resolve(labelsByCode, discount.DiscountTypeCode),
                    Amount = -discountAmount,
                    FeeInvoice = invoice
                };
                invoice.Lines.Add(discountLine);
            }

            foreach (var scholarship in scholarships)
            {
                var scholarshipAmount = ResolveAwardAmount(scholarship.ValueType, scholarship.Value, recurringSubtotal);
                if (scholarshipAmount <= 0m)
                {
                    continue;
                }

                var scholarshipLine = new FeeInvoiceLine
                {
                    FeeInvoiceId = invoice.Id,
                    Source = FeeLineSource.Scholarship,
                    StudentScholarshipId = scholarship.Id,
                    Description = "Scholarship - " + ConfigLabelHelper.Resolve(labelsByCode, scholarship.ScholarshipTypeCode),
                    Amount = -scholarshipAmount,
                    FeeInvoice = invoice
                };
                invoice.Lines.Add(scholarshipLine);
            }

            foreach (var adjustment in pendingAdjustments)
            {
                var adjustmentBase = recurringSubtotal;
                if (!string.IsNullOrWhiteSpace(adjustment.FeeCategoryCode))
                {
                    recurringSubtotalByCategory.TryGetValue(adjustment.FeeCategoryCode, out adjustmentBase);
                }

                var adjustmentAmount = ResolveAwardAmount(adjustment.ValueType, adjustment.Value, adjustmentBase);
                if (adjustmentAmount <= 0m)
                {
                    continue;
                }

                var signedAmount = adjustment.Direction == AdjustmentDirection.Increase ? adjustmentAmount : -adjustmentAmount;

                // CARRY_CORRECTION carries its own source-invoice reference in Remarks (crafted
                // by EnsureCarryForwardAdjustmentAsync, e.g. "Carried forward from INV20260008")
                // -- surfaced directly as the line description so the new invoice is
                // self-explanatory about where the charge came from, instead of the generic
                // "Adjustment - CARRY_CORRECTION".
                var adjustmentDescription = adjustment.AdjustmentTypeCode == FeeAdjustmentTypeCodes.CarryCorrection && !string.IsNullOrWhiteSpace(adjustment.Remarks)
                    ? adjustment.Remarks
                    : "Adjustment - " + ConfigLabelHelper.Resolve(labelsByCode, adjustment.AdjustmentTypeCode)
                        + (string.IsNullOrWhiteSpace(adjustment.FeeCategoryCode) ? string.Empty : " (" + ConfigLabelHelper.Resolve(labelsByCode, adjustment.FeeCategoryCode) + ")");

                var adjustmentLine = new FeeInvoiceLine
                {
                    FeeInvoiceId = invoice.Id,
                    Source = FeeLineSource.MonthlyAdjustment,
                    FeeAdjustmentId = adjustment.Id,
                    FeeCategoryCode = adjustment.FeeCategoryCode,
                    Description = adjustmentDescription,
                    Amount = signedAmount,
                    FeeInvoice = invoice
                };
                invoice.Lines.Add(adjustmentLine);

                if (stampAdjustments)
                {
                    adjustment.Status = AdjustmentStatus.Applied;
                    adjustment.AppliedFeeInvoiceId = invoice.Id;
                }
            }

            RecomputeTotals(invoice);
            return invoice;
        }

        // Stored totals are part of the snapshot: recomputed on every Draft edit and once more
        // at finalization, never derived on read. NetAmount floors at 0 (F4).
        public static void RecomputeTotals(FeeInvoice invoice)
        {
            decimal grossAmount = 0m;
            decimal discountAmount = 0m;

            foreach (var line in invoice.Lines)
            {
                if (line.Amount >= 0m)
                {
                    grossAmount += line.Amount;
                }
                else
                {
                    discountAmount += -line.Amount;
                }
            }

            invoice.GrossAmount = grossAmount;
            invoice.DiscountAmount = discountAmount;
            invoice.NetAmount = Math.Max(0m, grossAmount - discountAmount);
        }

        // Due date = the configured day of the billing month, clamped to the month's length.
        public static DateTime ResolveDueDate(int billingYear, int billingMonth, int dueDay)
        {
            var daysInMonth = DateTime.DaysInMonth(billingYear, billingMonth);
            var clampedDay = Math.Min(dueDay, daysInMonth);

            var dueDate = new DateTime(billingYear, billingMonth, clampedDay, 0, 0, 0, DateTimeKind.Utc);
            return dueDate;
        }

        private static bool IsBillingPeriodBefore(int year, int month, int targetYear, int targetMonth)
        {
            if (year != targetYear)
            {
                return year < targetYear;
            }

            return month < targetMonth;
        }

        private static decimal ResolveAwardAmount(AwardValueType valueType, decimal value, decimal recurringSubtotal)
        {
            return valueType == AwardValueType.Percentage
                ? Math.Round(recurringSubtotal * (value / 100m), 2)
                : value;
        }

    }
}
