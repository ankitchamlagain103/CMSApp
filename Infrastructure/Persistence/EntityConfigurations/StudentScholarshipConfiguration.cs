using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations
{
    public class StudentScholarshipConfiguration : SoftDeleteAuditableEntityConfiguration<StudentScholarship>
    {
        public override void Configure(EntityTypeBuilder<StudentScholarship> builder)
        {
            base.Configure(builder);

            builder.ToTable("student_scholarships", "dbo");

            builder.HasKey(s => s.Id);

            builder.Property(s => s.Id)
                    .HasColumnName("id");

            builder.Property(s => s.EnrollmentId)
                    .HasColumnName("enrollment_id")
                    .IsRequired();

            // Config code (TypeCode ConfigTypeCodes.ScholarshipType), not a database FK.
            builder.Property(s => s.ScholarshipTypeCode)
                    .HasColumnName("scholarship_type_code")
                    .IsRequired()
                    .HasMaxLength(100);

            builder.Property(s => s.ValueType)
                    .HasColumnName("value_type")
                    .IsRequired();

            builder.Property(s => s.Value)
                    .HasColumnName("value")
                    .HasColumnType("decimal(10,2)")
                    .IsRequired();

            builder.Property(s => s.Remarks)
                    .HasColumnName("remarks")
                    .HasMaxLength(500);

            builder.HasOne(s => s.Enrollment)
                    .WithMany(e => e.Scholarships)
                    .HasForeignKey(s => s.EnrollmentId)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(s => s.EnrollmentId)
                    .HasDatabaseName("ix_student_scholarships_enrollment_id");
        }
    }
}
