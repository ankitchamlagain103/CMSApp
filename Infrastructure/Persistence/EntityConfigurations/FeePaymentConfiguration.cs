using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations
{
    public class FeePaymentConfiguration : SoftDeleteAuditableEntityConfiguration<FeePayment>
    {
        public override void Configure(EntityTypeBuilder<FeePayment> builder)
        {
            base.Configure(builder);

            builder.ToTable("fee_payments", "dbo");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.Id)
                    .HasColumnName("id");

            builder.Property(p => p.ReceiptNo)
                    .HasColumnName("receipt_no")
                    .IsRequired()
                    .HasMaxLength(50);

            builder.Property(p => p.EnrollmentId)
                    .HasColumnName("enrollment_id")
                    .IsRequired();

            builder.Property(p => p.PaymentDate)
                    .HasColumnName("payment_date")
                    .HasColumnType("date")
                    .IsRequired();

            builder.Property(p => p.Amount)
                    .HasColumnName("amount")
                    .HasColumnType("decimal(12,2)")
                    .IsRequired();

            builder.Property(p => p.PaymentMode)
                    .HasColumnName("payment_mode")
                    .IsRequired();

            builder.Property(p => p.ReferenceNo)
                    .HasColumnName("reference_no")
                    .HasMaxLength(100);

            builder.Property(p => p.Status)
                    .HasColumnName("status")
                    .IsRequired();

            builder.Property(p => p.Remarks)
                    .HasColumnName("remarks")
                    .HasMaxLength(500);

            builder.HasOne(p => p.Enrollment)
                    .WithMany()
                    .HasForeignKey(p => p.EnrollmentId)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(p => p.ReceiptNo)
                    .IsUnique()
                    .HasDatabaseName("ix_fee_payments_receipt_no");

            builder.HasIndex(p => new { p.EnrollmentId, p.PaymentDate })
                    .HasDatabaseName("ix_fee_payments_enrollment_date");
        }
    }
}
