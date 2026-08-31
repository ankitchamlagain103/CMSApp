using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations
{
    public class StudentExamMarkConfiguration : AuditableEntityConfiguration<StudentExamMark>
    {
        public override void Configure(EntityTypeBuilder<StudentExamMark> builder)
        {
            base.Configure(builder);

            builder.ToTable("student_exam_marks", "dbo");

            builder.HasKey(m => m.Id);

            builder.Property(m => m.Id)
                    .HasColumnName("id");

            builder.Property(m => m.ExamId)
                    .HasColumnName("exam_id")
                    .IsRequired();

            builder.Property(m => m.EnrollmentId)
                    .HasColumnName("enrollment_id")
                    .IsRequired();

            builder.Property(m => m.TheoryObtainedMarks)
                    .HasColumnName("theory_obtained_marks")
                    .HasColumnType("decimal(5,2)")
                    .IsRequired(false);

            builder.Property(m => m.PracticalObtainedMarks)
                    .HasColumnName("practical_obtained_marks")
                    .HasColumnType("decimal(5,2)")
                    .IsRequired(false);

            builder.Property(m => m.InternalMarks)
                    .HasColumnName("internal_marks")
                    .HasColumnType("decimal(5,2)")
                    .IsRequired(false);

            builder.Property(m => m.TheoryGraceMarks)
                    .HasColumnName("theory_grace_marks")
                    .HasColumnType("decimal(5,2)")
                    .HasDefaultValue(0m)
                    .IsRequired();

            builder.Property(m => m.PracticalGraceMarks)
                    .HasColumnName("practical_grace_marks")
                    .HasColumnType("decimal(5,2)")
                    .HasDefaultValue(0m)
                    .IsRequired();

            builder.Property(m => m.TheoryAbsent)
                    .HasColumnName("theory_absent")
                    .HasDefaultValue(false)
                    .IsRequired();

            builder.Property(m => m.PracticalAbsent)
                    .HasColumnName("practical_absent")
                    .HasDefaultValue(false)
                    .IsRequired();

            builder.Property(m => m.TotalMarks)
                    .HasColumnName("total_marks")
                    .HasColumnType("decimal(5,2)")
                    .IsRequired();

            builder.Property(m => m.Grade)
                    .HasColumnName("grade")
                    .HasMaxLength(5);

            builder.Property(m => m.GradePoint)
                    .HasColumnName("grade_point")
                    .HasColumnType("decimal(4,2)")
                    .IsRequired(false);

            builder.Property(m => m.Remarks)
                    .HasColumnName("remarks")
                    .HasMaxLength(300);

            builder.Property(m => m.IsAbsent)
                    .HasColumnName("is_absent")
                    .HasDefaultValue(false)
                    .IsRequired();

            builder.Property(m => m.IsPublished)
                    .HasColumnName("is_published")
                    .HasDefaultValue(false)
                    .IsRequired();

            builder.HasOne(m => m.Exam)
                    .WithMany(e => e.Marks)
                    .HasForeignKey(m => m.ExamId)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(m => m.Enrollment)
                    .WithMany()
                    .HasForeignKey(m => m.EnrollmentId)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(m => new { m.ExamId, m.EnrollmentId })
                    .IsUnique()
                    .HasDatabaseName("ix_student_exam_marks_exam_enrollment");
        }
    }
}
