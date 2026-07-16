using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations
{
    public class FeeStructureConfiguration : SoftDeleteAuditableEntityConfiguration<FeeStructure>
    {
        public override void Configure(EntityTypeBuilder<FeeStructure> builder)
        {
            base.Configure(builder);

            builder.ToTable("fee_structures", "dbo");

            builder.HasKey(f => f.Id);

            builder.Property(f => f.Id)
                    .HasColumnName("id");

            builder.Property(f => f.AcademicClassId)
                    .HasColumnName("academic_class_id")
                    .IsRequired();

            builder.Property(f => f.Status)
                    .HasColumnName("status")
                    .IsRequired();

            builder.HasOne(f => f.AcademicClass)
                    .WithMany()
                    .HasForeignKey(f => f.AcademicClassId)
                    .OnDelete(DeleteBehavior.Restrict);

            // One header per class -- IgnoreQueryFilters-checked in the service so a soft-deleted
            // row still reserves the class (same "possibly soft-deleted" pattern as
            // AcademicClass.CombinationExistsAsync).
            builder.HasIndex(f => f.AcademicClassId)
                    .IsUnique()
                    .HasDatabaseName("ix_fee_structures_academic_class");
        }
    }
}
