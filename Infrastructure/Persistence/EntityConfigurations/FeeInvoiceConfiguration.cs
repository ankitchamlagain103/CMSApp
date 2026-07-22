using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations
{
    public class FeeInvoiceConfiguration : SoftDeleteAuditableEntityConfiguration<FeeInvoice>
    {
        public override void Configure(EntityTypeBuilder<FeeInvoice> builder)
        {
            base.Configure(builder);

            builder.ToTable("fee_invoices", "dbo");

            builder.HasKey(i => i.Id);

            builder.Property(i => i.Id)
                    .HasColumnName("id");

            builder.Property(i => i.InvoiceNo)
                    .HasColumnName("invoice_no")
                    .IsRequired()
                    .HasMaxLength(50);

            builder.Property(i => i.EnrollmentId)
                    .HasColumnName("enrollment_id")
                    .IsRequired();

            builder.Property(i => i.AcademicYearId)
                    .HasColumnName("academic_year_id")
                    .IsRequired();

            builder.Property(i => i.BillingYear)
                    .HasColumnName("billing_year")
                    .IsRequired();

            builder.Property(i => i.BillingMonth)
                    .HasColumnName("billing_month")
                    .IsRequired();

            builder.Property(i => i.Status)
                    .HasColumnName("status")
                    .IsRequired();

            builder.Property(i => i.GrossAmount)
                    .HasColumnName("gross_amount")
                    .HasColumnType("decimal(12,2)")
                    .IsRequired();

            builder.Property(i => i.DiscountAmount)
                    .HasColumnName("discount_amount")
                    .HasColumnType("decimal(12,2)")
                    .IsRequired();

            builder.Property(i => i.NetAmount)
                    .HasColumnName("net_amount")
                    .HasColumnType("decimal(12,2)")
                    .IsRequired();

            builder.Property(i => i.PaidAmount)
                    .HasColumnName("paid_amount")
                    .HasColumnType("decimal(12,2)")
                    .IsRequired();

            builder.Property(i => i.PreviousDueAmount)
                    .HasColumnName("previous_due_amount")
                    .HasColumnType("decimal(12,2)")
                    .IsRequired();

            builder.Property(i => i.CarriedForwardAmount)
                    .HasColumnName("carried_forward_amount")
                    .HasColumnType("decimal(12,2)")
                    .IsRequired();

            builder.Property(i => i.CarriedForwardToInvoiceId)
                    .HasColumnName("carried_forward_to_invoice_id");

            builder.Property(i => i.DueDate)
                    .HasColumnName("due_date")
                    .HasColumnType("date")
                    .IsRequired();

            builder.Property(i => i.GeneratedTs)
                    .HasColumnName("generated_ts");

            builder.Property(i => i.Remarks)
                    .HasColumnName("remarks")
                    .HasMaxLength(500);

            builder.HasOne(i => i.Enrollment)
                    .WithMany()
                    .HasForeignKey(i => i.EnrollmentId)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(i => i.AcademicYear)
                    .WithMany()
                    .HasForeignKey(i => i.AcademicYearId)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(i => i.InvoiceNo)
                    .IsUnique()
                    .HasDatabaseName("ix_fee_invoices_invoice_no");

            // One live invoice per enrollment per billing month: Cancelled (6) and soft-deleted
            // rows are excluded so a cancelled month can be regenerated.
            builder.HasIndex(i => new { i.EnrollmentId, i.BillingYear, i.BillingMonth })
                    .IsUnique()
                    .HasDatabaseName("ix_fee_invoices_enrollment_period")
                    .HasFilter("status <> " + (int)FeeInvoiceStatus.Cancelled + " AND is_deleted = false");

            builder.HasIndex(i => new { i.AcademicYearId, i.BillingYear, i.BillingMonth })
                    .HasDatabaseName("ix_fee_invoices_year_period");

            builder.HasIndex(i => new { i.EnrollmentId, i.Status })
                    .HasDatabaseName("ix_fee_invoices_enrollment_status");
        }
    }
}
