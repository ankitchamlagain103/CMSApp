using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations
{
    public class EmployeeLeaveBalanceConfiguration : AuditableEntityConfiguration<EmployeeLeaveBalance>
    {
        public override void Configure(EntityTypeBuilder<EmployeeLeaveBalance> builder)
        {
            base.Configure(builder);

            builder.ToTable("employee_leave_balances", "dbo");

            builder.HasKey(b => b.Id);

            builder.Property(b => b.Id)
                    .HasColumnName("id");

            builder.Property(b => b.EmployeeId)
                    .HasColumnName("employee_id")
                    .IsRequired();

            builder.Property(b => b.LeaveTypeId)
                    .HasColumnName("leave_type_id")
                    .IsRequired();

            builder.Property(b => b.FiscalYearId)
                    .HasColumnName("fiscal_year_id")
                    .IsRequired();

            builder.Property(b => b.Allocated)
                    .HasColumnName("allocated")
                    .HasColumnType("decimal(6,2)")
                    .IsRequired();

            builder.Property(b => b.Used)
                    .HasColumnName("used")
                    .HasColumnType("decimal(6,2)")
                    .IsRequired();

            builder.Property(b => b.Pending)
                    .HasColumnName("pending")
                    .HasColumnType("decimal(6,2)")
                    .IsRequired();

            builder.Property(b => b.Balance)
                    .HasColumnName("balance")
                    .HasColumnType("decimal(6,2)")
                    .IsRequired();

            builder.HasOne(b => b.Employee)
                    .WithMany(e => e.LeaveBalances)
                    .HasForeignKey(b => b.EmployeeId)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(b => b.LeaveType)
                    .WithMany()
                    .HasForeignKey(b => b.LeaveTypeId)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(b => b.FiscalYear)
                    .WithMany()
                    .HasForeignKey(b => b.FiscalYearId)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(b => new { b.EmployeeId, b.LeaveTypeId, b.FiscalYearId })
                    .IsUnique()
                    .HasDatabaseName("ix_employee_leave_balances_employee_type_year");
        }
    }
}
