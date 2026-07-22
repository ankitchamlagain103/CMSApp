using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations
{
    public class SalaryAdjustmentConfiguration : SoftDeleteAuditableEntityConfiguration<SalaryAdjustment>
    {
        public override void Configure(EntityTypeBuilder<SalaryAdjustment> builder)
        {
            base.Configure(builder);

            builder.ToTable("salary_adjustments", "dbo");

            builder.HasKey(a => a.Id);

            builder.Property(a => a.Id)
                    .HasColumnName("id");

            builder.Property(a => a.EmployeeId)
                    .HasColumnName("employee_id")
                    .IsRequired();

            builder.Property(a => a.FiscalYearId)
                    .HasColumnName("fiscal_year_id")
                    .IsRequired();

            builder.Property(a => a.MonthIndex)
                    .HasColumnName("month_index")
                    .IsRequired();

            // Config code (TypeCode ConfigTypeCodes.SalaryAdjustmentType), not a database FK.
            builder.Property(a => a.AdjustmentTypeCode)
                    .HasColumnName("adjustment_type_code")
                    .IsRequired()
                    .HasMaxLength(100);

            builder.Property(a => a.Direction)
                    .HasColumnName("direction")
                    .IsRequired();

            builder.Property(a => a.ValueType)
                    .HasColumnName("value_type")
                    .IsRequired();

            builder.Property(a => a.Value)
                    .HasColumnName("value")
                    .HasColumnType("decimal(12,2)")
                    .IsRequired();

            builder.Property(a => a.Quantity)
                    .HasColumnName("quantity")
                    .HasColumnType("decimal(6,2)");

            builder.Property(a => a.Remarks)
                    .HasColumnName("remarks")
                    .HasMaxLength(500);

            builder.Property(a => a.Status)
                    .HasColumnName("status")
                    .IsRequired();

            // Lineage scalar, no FK -- same snapshot reasoning as FeeInvoiceLine.
            builder.Property(a => a.AppliedSalarySlipId)
                    .HasColumnName("applied_salary_slip_id");

            builder.HasOne(a => a.Employee)
                    .WithMany()
                    .HasForeignKey(a => a.EmployeeId)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(a => a.FiscalYear)
                    .WithMany()
                    .HasForeignKey(a => a.FiscalYearId)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(a => new { a.EmployeeId, a.FiscalYearId, a.MonthIndex, a.Status })
                    .HasDatabaseName("ix_salary_adjustments_employee_period_status");
        }
    }
}
