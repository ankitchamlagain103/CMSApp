using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations
{
    public class EmployeeQualificationConfiguration : AuditableEntityConfiguration<EmployeeQualification>
    {
        public override void Configure(EntityTypeBuilder<EmployeeQualification> builder)
        {
            base.Configure(builder);

            builder.ToTable("employee_qualifications", "dbo");

            builder.HasKey(q => q.Id);

            builder.Property(q => q.Id)
                    .HasColumnName("id");

            builder.Property(q => q.EmployeeId)
                    .HasColumnName("employee_id")
                    .IsRequired();

            // Config code (TypeCode 1005), not a database FK.
            builder.Property(q => q.QualificationCode)
                    .HasColumnName("qualification_code")
                    .IsRequired()
                    .HasMaxLength(100);

            builder.Property(q => q.CourseName)
                    .HasColumnName("course_name")
                    .HasMaxLength(200);

            builder.Property(q => q.Institution)
                    .HasColumnName("institution")
                    .HasMaxLength(255);

            builder.Property(q => q.CompletionYear)
                    .HasColumnName("completion_year");

            builder.Property(q => q.Score)
                    .HasColumnName("score")
                    .HasMaxLength(50);

            builder.Property(q => q.Remarks)
                    .HasColumnName("remarks")
                    .HasMaxLength(500);

            builder.HasOne(q => q.Employee)
                    .WithMany(e => e.Qualifications)
                    .HasForeignKey(q => q.EmployeeId)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(q => q.EmployeeId)
                    .HasDatabaseName("ix_employee_qualifications_employee_id");
        }
    }
}
