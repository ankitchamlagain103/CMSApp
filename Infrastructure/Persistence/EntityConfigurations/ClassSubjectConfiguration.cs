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

            // DB-enforced "mandatory subjects are always class-wide" -- previously an app-only
            // rule (AcademicClassService.AssignSubjectAsync), now backed by a real constraint too.
            // The marks-range constraint backs the same "pass <= full" rule the validator checks
            // on write (2026-07-15).
            builder.ToTable("class_subjects", "dbo", t =>
            {
                t.HasCheckConstraint("ck_class_subjects_mandatory_classwide", "is_mandatory = false OR class_section_id IS NULL");
                t.HasCheckConstraint("ck_class_subjects_marks_range", "pass_marks IS NULL OR full_marks IS NULL OR pass_marks <= full_marks");
            });

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

            builder.Property(cs => cs.CreditHours)
                    .HasColumnName("credit_hours")
                    .HasColumnType("decimal(4,2)")
                    .IsRequired(false);

            builder.Property(cs => cs.FullMarks)
                    .HasColumnName("full_marks")
                    .IsRequired(false);

            builder.Property(cs => cs.PassMarks)
                    .HasColumnName("pass_marks")
                    .IsRequired(false);

            builder.Property(cs => cs.TheoryMarks)
                    .HasColumnName("theory_marks")
                    .IsRequired(false);

            builder.Property(cs => cs.PracticalMarks)
                    .HasColumnName("practical_marks")
                    .IsRequired(false);

            builder.HasOne(cs => cs.AcademicClass)
                    .WithMany(c => c.ClassSubjects)
                    .HasForeignKey(cs => cs.AcademicClassId)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(cs => cs.ClassSection)
                    .WithMany()
                    .HasForeignKey(cs => cs.ClassSectionId)
                    .OnDelete(DeleteBehavior.Restrict);

            // Postgres treats NULLs as distinct in unique indexes, so this index alone can't stop
            // a duplicate class-wide (NULL-section) row -- the partial index below closes that
            // specific gap; the service's row lookup pre-check still covers the remaining
            // "class-wide XOR per-section" cross-partition rule (no single index/CHECK can
            // express "this subject code has zero rows in the other partition").
            builder.HasIndex(cs => new { cs.AcademicClassId, cs.SubjectCode, cs.ClassSectionId })
                    .IsUnique()
                    .HasDatabaseName("ix_class_subjects_class_subject_section");

            // DB-enforced half of the "at most one class-wide row per subject per class" rule --
            // a plain unique index can't do this because NULLs are distinct (see above), but a
            // partial index scoped to the NULL-section rows can.
            builder.HasIndex(cs => new { cs.AcademicClassId, cs.SubjectCode })
                    .IsUnique()
                    .HasFilter("class_section_id IS NULL")
                    .HasDatabaseName("ix_class_subjects_classwide_unique");
        }
    }
}
