using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations
{
    public class TeacherAssignmentConfiguration : AuditableEntityConfiguration<TeacherAssignment>
    {
        public override void Configure(EntityTypeBuilder<TeacherAssignment> builder)
        {
            base.Configure(builder);

            builder.ToTable("teacher_assignments", "dbo");

            builder.HasKey(a => a.Id);

            builder.Property(a => a.Id)
                    .HasColumnName("id");

            builder.Property(a => a.TeacherId)
                    .HasColumnName("teacher_id")
                    .IsRequired();

            builder.Property(a => a.ClassSubjectId)
                    .HasColumnName("class_subject_id")
                    .IsRequired();

            // Null = the assignment covers every section of the ClassSubject's class.
            builder.Property(a => a.ClassSectionId)
                    .HasColumnName("class_section_id")
                    .IsRequired(false);

            builder.Property(a => a.IsClassTeacher)
                    .HasColumnName("is_class_teacher")
                    .HasDefaultValue(false);

            builder.HasOne(a => a.Teacher)
                    .WithMany(t => t.Assignments)
                    .HasForeignKey(a => a.TeacherId)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(a => a.ClassSubject)
                    .WithMany()
                    .HasForeignKey(a => a.ClassSubjectId)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(a => a.ClassSection)
                    .WithMany()
                    .HasForeignKey(a => a.ClassSectionId)
                    .OnDelete(DeleteBehavior.Restrict);

            // Postgres treats NULLs as distinct in unique indexes, so this index alone can't stop
            // duplicate (teacher, subject, NULL) rows -- the service's AssignmentExistsAsync
            // pre-check covers that case.
            builder.HasIndex(a => new { a.TeacherId, a.ClassSubjectId, a.ClassSectionId })
                    .IsUnique()
                    .HasDatabaseName("ix_teacher_assignments_teacher_subject_section");
        }
    }
}
