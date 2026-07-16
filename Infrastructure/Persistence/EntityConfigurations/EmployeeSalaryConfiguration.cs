using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations
{
    public class EmployeeSalaryConfiguration : SoftDeleteAuditableEntityConfiguration<EmployeeSalary>
    {
        public override void Configure(EntityTypeBuilder<EmployeeSalary> builder)
        {
            base.Configure(builder);

            builder.ToTable("employee_salaries", "dbo");

            builder.HasKey(s => s.Id);

            builder.Property(s => s.Id)
                    .HasColumnName("id");

            builder.Property(s => s.EmployeeId)
                    .HasColumnName("employee_id")
                    .IsRequired();

            builder.Property(s => s.EffectiveFromDate)
                    .HasColumnName("effective_from_date")
                    .HasColumnType("date")
                    .IsRequired();

            builder.Property(s => s.AssessmentType)
                    .HasColumnName("assessment_type")
                    .IsRequired();

            builder.HasOne(s => s.Employee)
                    .WithMany(e => e.Salaries)
                    .HasForeignKey(s => s.EmployeeId)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(s => new { s.EmployeeId, s.EffectiveFromDate })
                    .IsUnique()
                    .HasDatabaseName("ix_employee_salaries_employee_effective_date");
        }
    }
}
