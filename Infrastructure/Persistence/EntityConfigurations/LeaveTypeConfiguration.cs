using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations
{
    public class LeaveTypeConfiguration : SoftDeleteAuditableEntityConfiguration<LeaveType>
    {
        public override void Configure(EntityTypeBuilder<LeaveType> builder)
        {
            base.Configure(builder);

            builder.ToTable("leave_types", "dbo");

            builder.HasKey(t => t.Id);

            builder.Property(t => t.Id)
                    .HasColumnName("id");

            builder.Property(t => t.Name)
                    .HasColumnName("name")
                    .IsRequired()
                    .HasMaxLength(100);

            builder.Property(t => t.DaysPerYear)
                    .HasColumnName("days_per_year")
                    .HasColumnType("decimal(6,2)")
                    .IsRequired();

            builder.Property(t => t.CarryForward)
                    .HasColumnName("carry_forward")
                    .IsRequired();

            builder.Property(t => t.IsPaid)
                    .HasColumnName("is_paid")
                    .IsRequired();

            // Policy configuration (2026-07-24) -- both optional.
            builder.Property(t => t.MaxConsecutiveDays)
                    .HasColumnName("max_consecutive_days");

            builder.Property(t => t.MaxDaysPerWeek)
                    .HasColumnName("max_days_per_week")
                    .HasColumnType("decimal(6,2)");

            builder.Property(t => t.MaxDaysPerMonth)
                    .HasColumnName("max_days_per_month")
                    .HasColumnType("decimal(6,2)");

            builder.HasIndex(t => t.Name)
                    .IsUnique()
                    .HasDatabaseName("ix_leave_types_name");
        }
    }
}
