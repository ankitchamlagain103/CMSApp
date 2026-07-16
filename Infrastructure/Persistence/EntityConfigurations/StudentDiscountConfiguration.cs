using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations
{
    public class StudentDiscountConfiguration : SoftDeleteAuditableEntityConfiguration<StudentDiscount>
    {
        public override void Configure(EntityTypeBuilder<StudentDiscount> builder)
        {
            base.Configure(builder);

            builder.ToTable("student_discounts", "dbo");

            builder.HasKey(d => d.Id);

            builder.Property(d => d.Id)
                    .HasColumnName("id");

            builder.Property(d => d.EnrollmentId)
                    .HasColumnName("enrollment_id")
                    .IsRequired();

            // Config code (TypeCode ConfigTypeCodes.DiscountType), not a database FK -- same
            // convention as every other Config-backed code column in this feature.
            builder.Property(d => d.DiscountTypeCode)
                    .HasColumnName("discount_type_code")
                    .IsRequired()
                    .HasMaxLength(100);

            builder.Property(d => d.ValueType)
                    .HasColumnName("value_type")
                    .IsRequired();

            builder.Property(d => d.Value)
                    .HasColumnName("value")
                    .HasColumnType("decimal(10,2)")
                    .IsRequired();

            builder.Property(d => d.Remarks)
                    .HasColumnName("remarks")
                    .HasMaxLength(500);

            builder.HasOne(d => d.Enrollment)
                    .WithMany(e => e.Discounts)
                    .HasForeignKey(d => d.EnrollmentId)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(d => d.EnrollmentId)
                    .HasDatabaseName("ix_student_discounts_enrollment_id");
        }
    }
}
