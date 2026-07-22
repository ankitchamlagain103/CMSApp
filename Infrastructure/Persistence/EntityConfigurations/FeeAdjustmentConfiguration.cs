using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations
{
    public class FeeAdjustmentConfiguration : SoftDeleteAuditableEntityConfiguration<FeeAdjustment>
    {
        public override void Configure(EntityTypeBuilder<FeeAdjustment> builder)
        {
            base.Configure(builder);

            builder.ToTable("fee_adjustments", "dbo");

            builder.HasKey(a => a.Id);

            builder.Property(a => a.Id)
                    .HasColumnName("id");

            builder.Property(a => a.EnrollmentId)
                    .HasColumnName("enrollment_id")
                    .IsRequired();

            builder.Property(a => a.BillingYear)
                    .HasColumnName("billing_year")
                    .IsRequired();

            builder.Property(a => a.BillingMonth)
                    .HasColumnName("billing_month")
                    .IsRequired();

            // Config code (TypeCode ConfigTypeCodes.FeeAdjustmentType), not a database FK.
            builder.Property(a => a.AdjustmentTypeCode)
                    .HasColumnName("adjustment_type_code")
                    .IsRequired()
                    .HasMaxLength(100);

            // Optional Config code (TypeCode ConfigTypeCodes.FeeCategory), not a database FK --
            // same convention as FeeStructureItem.FeeCategoryCode/FeeRule.FeeCategoryCode.
            builder.Property(a => a.FeeCategoryCode)
                    .HasColumnName("fee_category_code")
                    .HasMaxLength(100);

            builder.Property(a => a.Direction)
                    .HasColumnName("direction")
                    .IsRequired();

            builder.Property(a => a.ValueType)
                    .HasColumnName("value_type")
                    .IsRequired();

            builder.Property(a => a.Value)
                    .HasColumnName("value")
                    .HasColumnType("decimal(12,2)")
                    .IsRequired();

            builder.Property(a => a.Remarks)
                    .HasColumnName("remarks")
                    .HasMaxLength(500);

            builder.Property(a => a.Status)
                    .HasColumnName("status")
                    .IsRequired();

            // Lineage scalar, no FK -- same snapshot reasoning as FeeInvoiceLine.
            builder.Property(a => a.AppliedFeeInvoiceId)
                    .HasColumnName("applied_fee_invoice_id");

            builder.HasOne(a => a.Enrollment)
                    .WithMany()
                    .HasForeignKey(a => a.EnrollmentId)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(a => new { a.EnrollmentId, a.BillingYear, a.BillingMonth, a.Status })
                    .HasDatabaseName("ix_fee_adjustments_enrollment_period_status");
        }
    }
}
