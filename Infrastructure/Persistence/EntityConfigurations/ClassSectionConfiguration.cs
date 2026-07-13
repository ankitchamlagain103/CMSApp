using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations
{
    public class ClassSectionConfiguration : SoftDeleteAuditableEntityConfiguration<ClassSection>
    {
        public override void Configure(EntityTypeBuilder<ClassSection> builder)
        {
            base.Configure(builder);

            builder.ToTable("class_sections", "dbo");

            builder.HasKey(s => s.Id);

            builder.Property(s => s.Id)
                    .HasColumnName("id");

            builder.Property(s => s.AcademicClassId)
                    .HasColumnName("academic_class_id")
                    .IsRequired();

            // Config code (TypeCode 1002), not a database FK -- see AcademicClassConfiguration.
            builder.Property(s => s.SectionCode)
                    .HasColumnName("section_code")
                    .IsRequired()
                    .HasMaxLength(100);

            builder.Property(s => s.Capacity)
                    .HasColumnName("capacity")
                    .IsRequired();

            builder.Property(s => s.Status)
                    .HasColumnName("status")
                    .IsRequired();

            builder.HasOne(s => s.AcademicClass)
                    .WithMany(c => c.Sections)
                    .HasForeignKey(s => s.AcademicClassId)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(s => new { s.AcademicClassId, s.SectionCode })
                    .IsUnique()
                    .HasDatabaseName("ix_class_sections_class_section");
        }
    }
}
