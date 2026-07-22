using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations
{
    public class FeeInvoiceLineConfiguration : AuditableEntityConfiguration<FeeInvoiceLine>
    {
        public override void Configure(EntityTypeBuilder<FeeInvoiceLine> builder)
        {
            base.Configure(builder);

            builder.ToTable("fee_invoice_lines", "dbo");

            builder.HasKey(l => l.Id);

            builder.Property(l => l.Id)
                    .HasColumnName("id");

            builder.Property(l => l.FeeInvoiceId)
                    .HasColumnName("fee_invoice_id")
                    .IsRequired();

            builder.Property(l => l.Source)
                    .HasColumnName("source")
                    .IsRequired();

            // Lineage columns: plain scalars, deliberately no FK constraints -- see the entity's
            // header comment (snapshots must not block configuration edits/deletes).
            builder.Property(l => l.FeeStructureItemId)
                    .HasColumnName("fee_structure_item_id");

            builder.Property(l => l.StudentDiscountId)
                    .HasColumnName("student_discount_id");

            builder.Property(l => l.StudentScholarshipId)
                    .HasColumnName("student_scholarship_id");

            builder.Property(l => l.FeeRuleId)
                    .HasColumnName("fee_rule_id");

            builder.Property(l => l.FeeAdjustmentId)
                    .HasColumnName("fee_adjustment_id");

            builder.Property(l => l.FeeCategoryCode)
                    .HasColumnName("fee_category_code")
                    .HasMaxLength(100);

            builder.Property(l => l.Description)
                    .HasColumnName("description")
                    .IsRequired()
                    .HasMaxLength(300);

            builder.Property(l => l.Amount)
                    .HasColumnName("amount")
                    .HasColumnType("decimal(12,2)")
                    .IsRequired();

            // Lines have no existence independent of their invoice.
            builder.HasOne(l => l.FeeInvoice)
                    .WithMany(i => i.Lines)
                    .HasForeignKey(l => l.FeeInvoiceId)
                    .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(l => l.FeeInvoiceId)
                    .HasDatabaseName("ix_fee_invoice_lines_invoice");
        }
    }
}
