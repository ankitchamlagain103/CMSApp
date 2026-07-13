using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations
{
    public class StudentConfiguration : SoftDeleteAuditableEntityConfiguration<Student>
    {
        public override void Configure(EntityTypeBuilder<Student> builder)
        {
            base.Configure(builder);

            builder.ToTable("students", "dbo");

            builder.HasKey(s => s.Id);

            builder.Property(s => s.Id)
                    .HasColumnName("id");

            builder.Property(s => s.AdmissionNo)
                    .HasColumnName("admission_no")
                    .IsRequired()
                    .HasMaxLength(30);

            builder.Property(s => s.FirstName)
                    .HasColumnName("first_name")
                    .IsRequired()
                    .HasMaxLength(100);

            builder.Property(s => s.MiddleName)
                    .HasColumnName("middle_name")
                    .HasMaxLength(100);

            builder.Property(s => s.LastName)
                    .HasColumnName("last_name")
                    .IsRequired()
                    .HasMaxLength(100);

            builder.Property(s => s.Gender)
                    .HasColumnName("gender")
                    .IsRequired();

            builder.Property(s => s.DateOfBirth)
                    .HasColumnName("date_of_birth")
                    .HasColumnType("date");

            builder.Property(s => s.Email)
                    .HasColumnName("email")
                    .HasMaxLength(255);

            builder.Property(s => s.Phone)
                    .HasColumnName("phone")
                    .HasMaxLength(20);

            builder.Property(s => s.Address)
                    .HasColumnName("address")
                    .HasMaxLength(500);

            builder.Property(s => s.AdmissionDate)
                    .HasColumnName("admission_date")
                    .HasColumnType("date");

            builder.Property(s => s.Status)
                    .HasColumnName("status")
                    .IsRequired();

            builder.HasIndex(s => s.AdmissionNo)
                    .IsUnique()
                    .HasDatabaseName("ix_students_admission_no");
        }
    }
}
