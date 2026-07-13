using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations
{
    public class EnrollmentConfiguration : SoftDeleteAuditableEntityConfiguration<Enrollment>
    {
        public override void Configure(EntityTypeBuilder<Enrollment> builder)
        {
            base.Configure(builder);

            builder.ToTable("enrollments", "dbo");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Id)
                    .HasColumnName("id");

            builder.Property(e => e.StudentId)
                    .HasColumnName("student_id")
                    .IsRequired();

            builder.Property(e => e.ClassSectionId)
                    .HasColumnName("class_section_id")
                    .IsRequired();

            builder.Property(e => e.RollNumber)
                    .HasColumnName("roll_number")
                    .HasMaxLength(20);

            builder.Property(e => e.EnrollmentDate)
                    .HasColumnName("enrollment_date")
                    .HasColumnType("date");

            builder.Property(e => e.Status)
                    .HasColumnName("status")
                    .IsRequired();

            builder.HasOne(e => e.Student)
                    .WithMany(s => s.Enrollments)
                    .HasForeignKey(e => e.StudentId)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.ClassSection)
                    .WithMany(s => s.Enrollments)
                    .HasForeignKey(e => e.ClassSectionId)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(e => new { e.StudentId, e.ClassSectionId })
                    .IsUnique()
                    .HasDatabaseName("ix_enrollments_student_section");

            builder.HasIndex(e => e.ClassSectionId)
                    .HasDatabaseName("ix_enrollments_class_section_id");
        }
    }
}
