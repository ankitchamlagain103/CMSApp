using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations
{
    public class FeePaymentAllocationConfiguration : AuditableEntityConfiguration<FeePaymentAllocation>
    {
        public override void Configure(EntityTypeBuilder<FeePaymentAllocation> builder)
        {
            base.Configure(builder);

            builder.ToTable("fee_payment_allocations", "dbo");

            builder.HasKey(a => a.Id);

            builder.Property(a => a.Id)
                    .HasColumnName("id");

            builder.Property(a => a.FeePaymentId)
                    .HasColumnName("fee_payment_id")
                    .IsRequired();

            builder.Property(a => a.FeeInvoiceId)
                    .HasColumnName("fee_invoice_id")
                    .IsRequired();

            builder.Property(a => a.Amount)
                    .HasColumnName("amount")
                    .HasColumnType("decimal(12,2)")
                    .IsRequired();

            // Allocations belong to their payment (cascade); the invoice side is Restrict so an
            // invoice with money against it can never be hard-removed out from under the ledger.
            builder.HasOne(a => a.FeePayment)
                    .WithMany(p => p.Allocations)
                    .HasForeignKey(a => a.FeePaymentId)
                    .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(a => a.FeeInvoice)
                    .WithMany(i => i.Allocations)
                    .HasForeignKey(a => a.FeeInvoiceId)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(a => new { a.FeePaymentId, a.FeeInvoiceId })
                    .IsUnique()
                    .HasDatabaseName("ix_fee_payment_allocations_payment_invoice");

            builder.HasIndex(a => a.FeeInvoiceId)
                    .HasDatabaseName("ix_fee_payment_allocations_invoice");
        }
    }
}
