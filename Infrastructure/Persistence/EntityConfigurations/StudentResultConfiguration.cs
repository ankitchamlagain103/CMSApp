using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations
{
    public class StudentResultConfiguration : SoftDeleteAuditableEntityConfiguration<StudentResult>
    {
        public override void Configure(EntityTypeBuilder<StudentResult> builder)
        {
            base.Configure(builder);

            builder.ToTable("student_results", "dbo");

            builder.HasKey(r => r.Id);

            builder.Property(r => r.Id)
                    .HasColumnName("id");

            builder.Property(r => r.EnrollmentId)
                    .HasColumnName("enrollment_id")
                    .IsRequired();

            builder.Property(r => r.ExamTermId)
                    .HasColumnName("exam_term_id")
                    .IsRequired();

            builder.Property(r => r.TotalMarks)
                    .HasColumnName("total_marks")
                    .HasColumnType("decimal(7,2)")
                    .IsRequired();

            builder.Property(r => r.ObtainedMarks)
                    .HasColumnName("obtained_marks")
                    .HasColumnType("decimal(7,2)")
                    .IsRequired();

            builder.Property(r => r.Percentage)
                    .HasColumnName("percentage")
                    .HasColumnType("decimal(5,2)")
                    .IsRequired();

            builder.Property(r => r.GPA)
                    .HasColumnName("gpa")
                    .HasColumnType("decimal(4,2)")
                    .IsRequired();

            builder.Property(r => r.Rank)
                    .HasColumnName("rank")
                    .IsRequired(false);

            builder.Property(r => r.ResultStatus)
                    .HasColumnName("result_status")
                    .IsRequired();

            builder.Property(r => r.PublishedDate)
                    .HasColumnName("published_date")
                    .IsRequired(false);

            builder.Property(r => r.Remarks)
                    .HasColumnName("remarks")
                    .HasMaxLength(500);

            builder.HasOne(r => r.Enrollment)
                    .WithMany()
                    .HasForeignKey(r => r.EnrollmentId)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(r => r.ExamTerm)
                    .WithMany()
                    .HasForeignKey(r => r.ExamTermId)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(r => new { r.EnrollmentId, r.ExamTermId })
                    .IsUnique()
                    .HasDatabaseName("ix_student_results_enrollment_term");
        }
    }
}
