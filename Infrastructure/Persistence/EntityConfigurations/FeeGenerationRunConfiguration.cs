using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations
{
    public class FeeGenerationRunConfiguration : SoftDeleteAuditableEntityConfiguration<FeeGenerationRun>
    {
        public override void Configure(EntityTypeBuilder<FeeGenerationRun> builder)
        {
            base.Configure(builder);

            builder.ToTable("fee_generation_runs", "dbo");

            builder.HasKey(r => r.Id);

            builder.Property(r => r.Id)
                    .HasColumnName("id");

            builder.Property(r => r.AcademicYearId)
                    .HasColumnName("academic_year_id")
                    .IsRequired();

            builder.Property(r => r.BillingYear)
                    .HasColumnName("billing_year")
                    .IsRequired();

            builder.Property(r => r.BillingMonth)
                    .HasColumnName("billing_month")
                    .IsRequired();

            builder.Property(r => r.GeneratedTs)
                    .HasColumnName("generated_ts")
                    .IsRequired();

            builder.Property(r => r.LastRegeneratedTs)
                    .HasColumnName("last_regenerated_ts");

            builder.Property(r => r.Remarks)
                    .HasColumnName("remarks")
                    .HasMaxLength(500);

            builder.HasOne(r => r.AcademicYear)
                    .WithMany()
                    .HasForeignKey(r => r.AcademicYearId)
                    .OnDelete(DeleteBehavior.Restrict);

            // One live run per period: soft-deleted rows are excluded so a removed run's period
            // can be regenerated.
            builder.HasIndex(r => new { r.AcademicYearId, r.BillingYear, r.BillingMonth })
                    .IsUnique()
                    .HasDatabaseName("ix_fee_generation_runs_year_period")
                    .HasFilter("is_deleted = false");
        }
    }
}
