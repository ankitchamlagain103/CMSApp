using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations
{
    public class EmployeeSalaryComponentConfiguration : AuditableEntityConfiguration<EmployeeSalaryComponent>
    {
        public override void Configure(EntityTypeBuilder<EmployeeSalaryComponent> builder)
        {
            base.Configure(builder);

            builder.ToTable("employee_salary_components", "dbo");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.Id)
                    .HasColumnName("id");

            builder.Property(c => c.EmployeeSalaryId)
                    .HasColumnName("employee_salary_id")
                    .IsRequired();

            // Config code (TypeCode ConfigTypeCodes.SalaryComponentType), not a database FK.
            builder.Property(c => c.ComponentCode)
                    .HasColumnName("component_code")
                    .IsRequired()
                    .HasMaxLength(100);

            builder.Property(c => c.ValueType)
                    .HasColumnName("value_type")
                    .IsRequired();

            builder.Property(c => c.Value)
                    .HasColumnName("value")
                    .HasColumnType("decimal(12,2)")
                    .IsRequired();

            builder.Property(c => c.FrequencyType)
                    .HasColumnName("frequency_type")
                    .IsRequired();

            builder.Property(c => c.IsTaxable)
                    .HasColumnName("is_taxable")
                    .HasDefaultValue(true);

            builder.Property(c => c.IsRetirementContribution)
                    .HasColumnName("is_retirement_contribution")
                    .HasDefaultValue(false);

            builder.HasOne(c => c.EmployeeSalary)
                    .WithMany(s => s.Components)
                    .HasForeignKey(c => c.EmployeeSalaryId)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(c => new { c.EmployeeSalaryId, c.ComponentCode })
                    .IsUnique()
                    .HasDatabaseName("ix_employee_salary_components_salary_component");
        }
    }
}
