using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations
{
    public class GradeScaleConfiguration : SoftDeleteAuditableEntityConfiguration<GradeScale>
    {
        public override void Configure(EntityTypeBuilder<GradeScale> builder)
        {
            base.Configure(builder);

            builder.ToTable("grade_scales", "dbo");

            builder.HasKey(g => g.Id);

            builder.Property(g => g.Id)
                    .HasColumnName("id");

            builder.Property(g => g.Grade)
                    .HasColumnName("grade")
                    .IsRequired()
                    .HasMaxLength(5);

            builder.Property(g => g.MinPercent)
                    .HasColumnName("min_percent")
                    .HasColumnType("decimal(5,2)")
                    .IsRequired();

            builder.Property(g => g.MaxPercent)
                    .HasColumnName("max_percent")
                    .HasColumnType("decimal(5,2)")
                    .IsRequired();

            builder.Property(g => g.GradePoint)
                    .HasColumnName("grade_point")
                    .HasColumnType("decimal(4,2)")
                    .IsRequired();

            builder.Property(g => g.Remarks)
                    .HasColumnName("remarks")
                    .HasMaxLength(100);

            // IgnoreQueryFilters in the service's uniqueness check still sees soft-deleted rows.
            builder.HasIndex(g => g.Grade)
                    .IsUnique()
                    .HasDatabaseName("ix_grade_scales_grade");
        }
    }
}
