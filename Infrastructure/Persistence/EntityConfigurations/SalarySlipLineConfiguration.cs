using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations
{
    public class SalarySlipLineConfiguration : AuditableEntityConfiguration<SalarySlipLine>
    {
        public override void Configure(EntityTypeBuilder<SalarySlipLine> builder)
        {
            base.Configure(builder);

            builder.ToTable("salary_slip_lines", "dbo");

            builder.HasKey(l => l.Id);

            builder.Property(l => l.Id)
                    .HasColumnName("id");

            builder.Property(l => l.SalarySlipId)
                    .HasColumnName("salary_slip_id")
                    .IsRequired();

            builder.Property(l => l.LineType)
                    .HasColumnName("line_type")
                    .IsRequired();

            builder.Property(l => l.Source)
                    .HasColumnName("source")
                    .IsRequired();

            builder.Property(l => l.ComponentCode)
                    .HasColumnName("component_code")
                    .HasMaxLength(100);

            // Lineage scalars, no FKs -- same snapshot reasoning as FeeInvoiceLine.
            builder.Property(l => l.SalaryAdjustmentId)
                    .HasColumnName("salary_adjustment_id");

            builder.Property(l => l.EmployeeLoanId)
                    .HasColumnName("employee_loan_id");

            builder.Property(l => l.Description)
                    .HasColumnName("description")
                    .IsRequired()
                    .HasMaxLength(300);

            builder.Property(l => l.Amount)
                    .HasColumnName("amount")
                    .HasColumnType("decimal(12,2)")
                    .IsRequired();

            builder.HasOne(l => l.SalarySlip)
                    .WithMany(s => s.Lines)
                    .HasForeignKey(l => l.SalarySlipId)
                    .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(l => l.SalarySlipId)
                    .HasDatabaseName("ix_salary_slip_lines_slip");
        }
    }
}
