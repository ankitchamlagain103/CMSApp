using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations
{
    public class StudentPromotionConfiguration : AuditableEntityConfiguration<StudentPromotion>
    {
        public override void Configure(EntityTypeBuilder<StudentPromotion> builder)
        {
            base.Configure(builder);

            builder.ToTable("student_promotions", "dbo");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.Id)
                    .HasColumnName("id");

            builder.Property(p => p.StudentId)
                    .HasColumnName("student_id")
                    .IsRequired();

            builder.Property(p => p.FromEnrollmentId)
                    .HasColumnName("from_enrollment_id")
                    .IsRequired();

            builder.Property(p => p.ToEnrollmentId)
                    .HasColumnName("to_enrollment_id")
                    .IsRequired();

            builder.Property(p => p.PromotionDate)
                    .HasColumnName("promotion_date")
                    .HasColumnType("date")
                    .IsRequired();

            builder.Property(p => p.PromotionType)
                    .HasColumnName("promotion_type")
                    .IsRequired();

            builder.Property(p => p.Remarks)
                    .HasColumnName("remarks")
                    .HasMaxLength(500);

            // Two FKs onto the same target type (Enrollment) -- both configured explicitly from
            // this, the "many", side so EF never has to guess which one a bare convention match
            // would apply to.
            builder.HasOne(p => p.Student)
                    .WithMany()
                    .HasForeignKey(p => p.StudentId)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(p => p.FromEnrollment)
                    .WithMany()
                    .HasForeignKey(p => p.FromEnrollmentId)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(p => p.ToEnrollment)
                    .WithMany()
                    .HasForeignKey(p => p.ToEnrollmentId)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(p => p.StudentId)
                    .HasDatabaseName("ix_student_promotions_student_id");

            builder.HasIndex(p => p.FromEnrollmentId)
                    .IsUnique()
                    .HasDatabaseName("ix_student_promotions_from_enrollment_id");
        }
    }
}
