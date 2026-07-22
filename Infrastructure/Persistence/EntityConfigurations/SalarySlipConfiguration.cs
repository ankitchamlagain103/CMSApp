using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations
{
    public class SalarySlipConfiguration : SoftDeleteAuditableEntityConfiguration<SalarySlip>
    {
        public override void Configure(EntityTypeBuilder<SalarySlip> builder)
        {
            base.Configure(builder);

            builder.ToTable("salary_slips", "dbo");

            builder.HasKey(s => s.Id);

            builder.Property(s => s.Id)
                    .HasColumnName("id");

            builder.Property(s => s.SlipNo)
                    .HasColumnName("slip_no")
                    .IsRequired()
                    .HasMaxLength(50);

            builder.Property(s => s.PayrollRunId)
                    .HasColumnName("payroll_run_id")
                    .IsRequired();

            builder.Property(s => s.EmployeeId)
                    .HasColumnName("employee_id")
                    .IsRequired();

            builder.Property(s => s.EmployeeSalaryId)
                    .HasColumnName("employee_salary_id")
                    .IsRequired();

            builder.Property(s => s.Status)
                    .HasColumnName("status")
                    .IsRequired();

            builder.Property(s => s.PeriodStartDate)
                    .HasColumnName("period_start_date")
                    .HasColumnType("date")
                    .IsRequired();

            builder.Property(s => s.PeriodEndDate)
                    .HasColumnName("period_end_date")
                    .HasColumnType("date")
                    .IsRequired();

            builder.Property(s => s.MonthDays)
                    .HasColumnName("month_days")
                    .IsRequired();

            builder.Property(s => s.PayDays)
                    .HasColumnName("pay_days")
                    .HasColumnType("decimal(5,2)")
                    .IsRequired();

            builder.Property(s => s.UnpaidLeaveDays)
                    .HasColumnName("unpaid_leave_days")
                    .HasColumnType("decimal(5,2)")
                    .IsRequired();

            builder.Property(s => s.GrossEarnings)
                    .HasColumnName("gross_earnings")
                    .HasColumnType("decimal(12,2)")
                    .IsRequired();

            builder.Property(s => s.TotalDeductions)
                    .HasColumnName("total_deductions")
                    .HasColumnType("decimal(12,2)")
                    .IsRequired();

            builder.Property(s => s.TaxAmount)
                    .HasColumnName("tax_amount")
                    .HasColumnType("decimal(12,2)")
                    .IsRequired();

            builder.Property(s => s.NetPay)
                    .HasColumnName("net_pay")
                    .HasColumnType("decimal(12,2)")
                    .IsRequired();

            builder.Property(s => s.Remarks)
                    .HasColumnName("remarks")
                    .HasMaxLength(500);

            builder.HasOne(s => s.PayrollRun)
                    .WithMany(r => r.Slips)
                    .HasForeignKey(s => s.PayrollRunId)
                    .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(s => s.Employee)
                    .WithMany()
                    .HasForeignKey(s => s.EmployeeId)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(s => s.EmployeeSalary)
                    .WithMany()
                    .HasForeignKey(s => s.EmployeeSalaryId)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(s => s.SlipNo)
                    .IsUnique()
                    .HasDatabaseName("ix_salary_slips_slip_no");

            builder.HasIndex(s => new { s.PayrollRunId, s.EmployeeId })
                    .IsUnique()
                    .HasDatabaseName("ix_salary_slips_run_employee");

            builder.HasIndex(s => s.EmployeeId)
                    .HasDatabaseName("ix_salary_slips_employee");
        }
    }
}
