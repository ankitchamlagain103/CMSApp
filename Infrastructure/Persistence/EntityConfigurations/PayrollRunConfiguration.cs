using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations
{
    public class PayrollRunConfiguration : SoftDeleteAuditableEntityConfiguration<PayrollRun>
    {
        public override void Configure(EntityTypeBuilder<PayrollRun> builder)
        {
            base.Configure(builder);

            builder.ToTable("payroll_runs", "dbo");

            builder.HasKey(r => r.Id);

            builder.Property(r => r.Id)
                    .HasColumnName("id");

            builder.Property(r => r.FiscalYearId)
                    .HasColumnName("fiscal_year_id")
                    .IsRequired();

            builder.Property(r => r.MonthIndex)
                    .HasColumnName("month_index")
                    .IsRequired();

            builder.Property(r => r.Status)
                    .HasColumnName("status")
                    .IsRequired();

            builder.Property(r => r.GeneratedTs)
                    .HasColumnName("generated_ts");

            builder.Property(r => r.ApprovedTs)
                    .HasColumnName("approved_ts");

            builder.Property(r => r.ApprovedBy)
                    .HasColumnName("approved_by")
                    .HasMaxLength(50);

            builder.Property(r => r.PaidTs)
                    .HasColumnName("paid_ts");

            builder.Property(r => r.Remarks)
                    .HasColumnName("remarks")
                    .HasMaxLength(500);

            builder.HasOne(r => r.FiscalYear)
                    .WithMany()
                    .HasForeignKey(r => r.FiscalYearId)
                    .OnDelete(DeleteBehavior.Restrict);

            // One live run per fiscal month: Cancelled (4) and soft-deleted rows are excluded
            // so a cancelled month can be regenerated.
            builder.HasIndex(r => new { r.FiscalYearId, r.MonthIndex })
                    .IsUnique()
                    .HasDatabaseName("ix_payroll_runs_fiscal_month")
                    .HasFilter("status <> " + (int)PayrollRunStatus.Cancelled + " AND is_deleted = false");
        }
    }
}
