using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations
{
    public class EmployeeInsurancePremiumConfiguration : AuditableEntityConfiguration<EmployeeInsurancePremium>
    {
        public override void Configure(EntityTypeBuilder<EmployeeInsurancePremium> builder)
        {
            base.Configure(builder);

            builder.ToTable("employee_insurance_premiums", "dbo");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.Id)
                    .HasColumnName("id");

            builder.Property(p => p.EmployeeSalaryId)
                    .HasColumnName("employee_salary_id")
                    .IsRequired();

            // Config code (TypeCode ConfigTypeCodes.InsuranceType); that Config row's
            // AdditionalValue1 carries the type's Nepal tax-deduction cap.
            builder.Property(p => p.InsuranceTypeCode)
                    .HasColumnName("insurance_type_code")
                    .IsRequired()
                    .HasMaxLength(100);

            builder.Property(p => p.AnnualPremiumAmount)
                    .HasColumnName("annual_premium_amount")
                    .HasColumnType("decimal(12,2)")
                    .IsRequired();

            builder.HasOne(p => p.EmployeeSalary)
                    .WithMany(s => s.InsurancePremiums)
                    .HasForeignKey(p => p.EmployeeSalaryId)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(p => new { p.EmployeeSalaryId, p.InsuranceTypeCode })
                    .IsUnique()
                    .HasDatabaseName("ix_employee_insurance_premiums_salary_type");
        }
    }
}
