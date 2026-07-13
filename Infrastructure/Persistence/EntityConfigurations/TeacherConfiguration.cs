using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations
{
    public class TeacherConfiguration : SoftDeleteAuditableEntityConfiguration<Teacher>
    {
        public override void Configure(EntityTypeBuilder<Teacher> builder)
        {
            base.Configure(builder);

            builder.ToTable("teachers", "dbo");

            builder.HasKey(t => t.Id);

            builder.Property(t => t.Id)
                    .HasColumnName("id");

            builder.Property(t => t.EmployeeNo)
                    .HasColumnName("employee_no")
                    .IsRequired()
                    .HasMaxLength(30);

            builder.Property(t => t.FirstName)
                    .HasColumnName("first_name")
                    .IsRequired()
                    .HasMaxLength(100);

            builder.Property(t => t.MiddleName)
                    .HasColumnName("middle_name")
                    .HasMaxLength(100);

            builder.Property(t => t.LastName)
                    .HasColumnName("last_name")
                    .IsRequired()
                    .HasMaxLength(100);

            builder.Property(t => t.Email)
                    .HasColumnName("email")
                    .HasMaxLength(255);

            builder.Property(t => t.Phone)
                    .HasColumnName("phone")
                    .HasMaxLength(20);

            builder.Property(t => t.JoiningDate)
                    .HasColumnName("joining_date")
                    .HasColumnType("date");

            builder.Property(t => t.Status)
                    .HasColumnName("status")
                    .IsRequired();

            builder.HasIndex(t => t.EmployeeNo)
                    .IsUnique()
                    .HasDatabaseName("ix_teachers_employee_no");
        }
    }
}
