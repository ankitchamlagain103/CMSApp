using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations
{
    public class EmployeeSalaryDeductionConfiguration : AuditableEntityConfiguration<EmployeeSalaryDeduction>
    {
        public override void Configure(EntityTypeBuilder<EmployeeSalaryDeduction> builder)
        {
            base.Configure(builder);

            builder.ToTable("employee_salary_deductions", "dbo");

            builder.HasKey(d => d.Id);

            builder.Property(d => d.Id)
                    .HasColumnName("id");

            builder.Property(d => d.EmployeeSalaryId)
                    .HasColumnName("employee_salary_id")
                    .IsRequired();

            // Config code (TypeCode ConfigTypeCodes.DeductionType), not a database FK.
            builder.Property(d => d.DeductionCode)
                    .HasColumnName("deduction_code")
                    .IsRequired()
                    .HasMaxLength(100);

            builder.Property(d => d.ValueType)
                    .HasColumnName("value_type")
                    .IsRequired();

            builder.Property(d => d.Value)
                    .HasColumnName("value")
                    .HasColumnType("decimal(12,2)")
                    .IsRequired();

            builder.Property(d => d.FrequencyType)
                    .HasColumnName("frequency_type")
                    .IsRequired();

            builder.Property(d => d.IsRetirementContribution)
                    .HasColumnName("is_retirement_contribution")
                    .HasDefaultValue(false);

            builder.HasOne(d => d.EmployeeSalary)
                    .WithMany(s => s.Deductions)
                    .HasForeignKey(d => d.EmployeeSalaryId)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(d => new { d.EmployeeSalaryId, d.DeductionCode })
                    .IsUnique()
                    .HasDatabaseName("ix_employee_salary_deductions_salary_deduction");
        }
    }
}
