using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations
{
    public class EnrollmentFeeSelectionConfiguration : AuditableEntityConfiguration<EnrollmentFeeSelection>
    {
        public override void Configure(EntityTypeBuilder<EnrollmentFeeSelection> builder)
        {
            base.Configure(builder);

            builder.ToTable("enrollment_fee_selections", "dbo");

            builder.HasKey(s => s.Id);

            builder.Property(s => s.Id)
                    .HasColumnName("id");

            builder.Property(s => s.EnrollmentId)
                    .HasColumnName("enrollment_id")
                    .IsRequired();

            builder.Property(s => s.FeeStructureItemId)
                    .HasColumnName("fee_structure_item_id")
                    .IsRequired();

            builder.HasOne(s => s.Enrollment)
                    .WithMany(e => e.FeeSelections)
                    .HasForeignKey(s => s.EnrollmentId)
                    .OnDelete(DeleteBehavior.Restrict);

            // Explicit mapping: the FK property name (FeeStructureItemId) doesn't match a
            // "FeeStructureItem" + "Id" nav-derived convention target on FeeStructureItem's own
            // side (no back-collection there), so this stays a one-directional FK from the "many"
            // side only, same pattern as RefreshToken.UserId.
            builder.HasOne(s => s.FeeStructureItem)
                    .WithMany()
                    .HasForeignKey(s => s.FeeStructureItemId)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(s => new { s.EnrollmentId, s.FeeStructureItemId })
                    .IsUnique()
                    .HasDatabaseName("ix_enrollment_fee_selections_enrollment_item");
        }
    }
}
