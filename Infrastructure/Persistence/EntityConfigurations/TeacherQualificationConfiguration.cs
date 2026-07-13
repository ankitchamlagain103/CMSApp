using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations
{
    public class TeacherQualificationConfiguration : AuditableEntityConfiguration<TeacherQualification>
    {
        public override void Configure(EntityTypeBuilder<TeacherQualification> builder)
        {
            base.Configure(builder);

            builder.ToTable("teacher_qualifications", "dbo");

            builder.HasKey(q => q.Id);

            builder.Property(q => q.Id)
                    .HasColumnName("id");

            builder.Property(q => q.TeacherId)
                    .HasColumnName("teacher_id")
                    .IsRequired();

            // Config code (TypeCode 1005), not a database FK.
            builder.Property(q => q.QualificationCode)
                    .HasColumnName("qualification_code")
                    .IsRequired()
                    .HasMaxLength(100);

            builder.Property(q => q.CourseName)
                    .HasColumnName("course_name")
                    .HasMaxLength(200);

            builder.Property(q => q.Institution)
                    .HasColumnName("institution")
                    .HasMaxLength(255);

            builder.Property(q => q.CompletionYear)
                    .HasColumnName("completion_year");

            builder.Property(q => q.Score)
                    .HasColumnName("score")
                    .HasMaxLength(50);

            builder.Property(q => q.Remarks)
                    .HasColumnName("remarks")
                    .HasMaxLength(500);

            builder.HasOne(q => q.Teacher)
                    .WithMany(t => t.Qualifications)
                    .HasForeignKey(q => q.TeacherId)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(q => q.TeacherId)
                    .HasDatabaseName("ix_teacher_qualifications_teacher_id");
        }
    }
}
