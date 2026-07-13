using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations
{
    public class ClassSubjectConfiguration : AuditableEntityConfiguration<ClassSubject>
    {
        public override void Configure(EntityTypeBuilder<ClassSubject> builder)
        {
            base.Configure(builder);

            builder.ToTable("class_subjects", "dbo");

            builder.HasKey(cs => cs.Id);

            builder.Property(cs => cs.Id)
                    .HasColumnName("id");

            builder.Property(cs => cs.AcademicClassId)
                    .HasColumnName("academic_class_id")
                    .IsRequired();

            // Null = offered to every section of the class; set = optional subject offered only
            // in that section.
            builder.Property(cs => cs.ClassSectionId)
                    .HasColumnName("class_section_id")
                    .IsRequired(false);

            // Config code (TypeCode 1003), not a database FK -- see AcademicClassConfiguration.
            builder.Property(cs => cs.SubjectCode)
                    .HasColumnName("subject_code")
                    .IsRequired()
                    .HasMaxLength(100);

            builder.Property(cs => cs.IsMandatory)
                    .HasColumnName("is_mandatory")
                    .HasDefaultValue(true);

            builder.Property(cs => cs.DisplayOrder)
                    .HasColumnName("display_order")
                    .IsRequired();

            builder.HasOne(cs => cs.AcademicClass)
                    .WithMany(c => c.ClassSubjects)
                    .HasForeignKey(cs => cs.AcademicClassId)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(cs => cs.ClassSection)
                    .WithMany()
                    .HasForeignKey(cs => cs.ClassSectionId)
                    .OnDelete(DeleteBehavior.Restrict);

            // Postgres treats NULLs as distinct in unique indexes, so this index alone can't stop
            // a duplicate class-wide (NULL-section) row -- the service's row lookup pre-check
            // covers that case, plus the "class-wide XOR per-section" rule.
            builder.HasIndex(cs => new { cs.AcademicClassId, cs.SubjectCode, cs.ClassSectionId })
                    .IsUnique()
                    .HasDatabaseName("ix_class_subjects_class_subject_section");
        }
    }
}
