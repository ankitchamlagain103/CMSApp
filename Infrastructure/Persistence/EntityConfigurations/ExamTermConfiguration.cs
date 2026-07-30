using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations
{
    public class ExamTermConfiguration : SoftDeleteAuditableEntityConfiguration<ExamTerm>
    {
        public override void Configure(EntityTypeBuilder<ExamTerm> builder)
        {
            base.Configure(builder);

            builder.ToTable("exam_terms", "dbo");

            builder.HasKey(t => t.Id);

            builder.Property(t => t.Id)
                    .HasColumnName("id");

            builder.Property(t => t.AcademicYearId)
                    .HasColumnName("academic_year_id")
                    .IsRequired();

            builder.Property(t => t.Code)
                    .HasColumnName("code")
                    .IsRequired()
                    .HasMaxLength(20);

            builder.Property(t => t.Name)
                    .HasColumnName("name")
                    .IsRequired()
                    .HasMaxLength(100);

            builder.Property(t => t.Sequence)
                    .HasColumnName("sequence")
                    .IsRequired();

            builder.Property(t => t.StartDate)
                    .HasColumnName("start_date")
                    .HasColumnType("date")
                    .IsRequired();

            builder.Property(t => t.EndDate)
                    .HasColumnName("end_date")
                    .HasColumnType("date")
                    .IsRequired();

            builder.Property(t => t.PublishResult)
                    .HasColumnName("publish_result")
                    .HasDefaultValue(false)
                    .IsRequired();

            builder.Property(t => t.Status)
                    .HasColumnName("status")
                    .IsRequired();

            builder.HasOne(t => t.AcademicYear)
                    .WithMany()
                    .HasForeignKey(t => t.AcademicYearId)
                    .OnDelete(DeleteBehavior.Restrict);

            // IgnoreQueryFilters in the service's uniqueness check still sees a soft-deleted row,
            // same convention as every other identity-bearing code column.
            builder.HasIndex(t => t.Code)
                    .IsUnique()
                    .HasDatabaseName("ix_exam_terms_code");

            builder.HasIndex(t => t.AcademicYearId)
                    .HasDatabaseName("ix_exam_terms_academic_year_id");
        }
    }
}
