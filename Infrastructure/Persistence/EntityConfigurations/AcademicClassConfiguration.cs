using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations
{
    public class AcademicClassConfiguration : SoftDeleteAuditableEntityConfiguration<AcademicClass>
    {
        public override void Configure(EntityTypeBuilder<AcademicClass> builder)
        {
            base.Configure(builder);

            builder.ToTable("academic_classes", "dbo");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.Id)
                    .HasColumnName("id");

            builder.Property(c => c.AcademicYearId)
                    .HasColumnName("academic_year_id")
                    .IsRequired();

            // grade_code holds a Config code (TypeCode 1001) -- deliberately NOT a database FK,
            // because Config.Code is only unique per TypeCode. Validated in AcademicClassService
            // against IUnitOfWork.Configs. Sections live in the child class_sections table.
            builder.Property(c => c.GradeCode)
                    .HasColumnName("grade_code")
                    .IsRequired()
                    .HasMaxLength(100);

            builder.Property(c => c.Status)
                    .HasColumnName("status")
                    .IsRequired();

            builder.HasOne(c => c.AcademicYear)
                    .WithMany(y => y.Classes)
                    .HasForeignKey(c => c.AcademicYearId)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(c => new { c.AcademicYearId, c.GradeCode })
                    .IsUnique()
                    .HasDatabaseName("ix_academic_classes_year_grade");
        }
    }
}
