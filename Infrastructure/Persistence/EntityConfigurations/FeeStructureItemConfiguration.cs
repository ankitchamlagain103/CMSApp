using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations
{
    public class FeeStructureItemConfiguration : AuditableEntityConfiguration<FeeStructureItem>
    {
        public override void Configure(EntityTypeBuilder<FeeStructureItem> builder)
        {
            base.Configure(builder);

            builder.ToTable("fee_structure_items", "dbo");

            builder.HasKey(i => i.Id);

            builder.Property(i => i.Id)
                    .HasColumnName("id");

            builder.Property(i => i.FeeStructureId)
                    .HasColumnName("fee_structure_id")
                    .IsRequired();

            // Config code (TypeCode ConfigTypeCodes.FeeCategory), not a database FK.
            builder.Property(i => i.FeeCategoryCode)
                    .HasColumnName("fee_category_code")
                    .IsRequired()
                    .HasMaxLength(100);

            builder.Property(i => i.Amount)
                    .HasColumnName("amount")
                    .HasColumnType("decimal(10,2)")
                    .IsRequired();

            builder.Property(i => i.FrequencyType)
                    .HasColumnName("frequency_type")
                    .IsRequired();

            builder.Property(i => i.InstallmentCount)
                    .HasColumnName("installment_count");

            builder.Property(i => i.IsOptional)
                    .HasColumnName("is_optional")
                    .HasDefaultValue(false);

            builder.Property(i => i.IsRefundable)
                    .HasColumnName("is_refundable")
                    .HasDefaultValue(false);

            // Items have no existence independent of their header -- a hard header delete takes
            // its items with it (the one real external reference, EnrollmentFeeSelection, is
            // guarded by a service-level check before that delete is ever allowed).
            builder.HasOne(i => i.FeeStructure)
                    .WithMany(f => f.Items)
                    .HasForeignKey(i => i.FeeStructureId)
                    .OnDelete(DeleteBehavior.Cascade);

            // A class's fee structure can't charge the same category twice.
            builder.HasIndex(i => new { i.FeeStructureId, i.FeeCategoryCode })
                    .IsUnique()
                    .HasDatabaseName("ix_fee_structure_items_structure_category");
        }
    }
}
