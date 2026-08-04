using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations
{
    public class TimePeriodConfiguration : SoftDeleteAuditableEntityConfiguration<TimePeriod>
    {
        public override void Configure(EntityTypeBuilder<TimePeriod> builder)
        {
            base.Configure(builder);

            builder.ToTable("time_periods", "dbo");

            builder.HasKey(t => t.Id);

            builder.Property(t => t.Id)
                    .HasColumnName("id");

            builder.Property(t => t.Name)
                    .HasColumnName("name")
                    .IsRequired()
                    .HasMaxLength(100);

            builder.Property(t => t.StartTime)
                    .HasColumnName("start_time")
                    .IsRequired();

            builder.Property(t => t.EndTime)
                    .HasColumnName("end_time")
                    .IsRequired();

            builder.Property(t => t.Kind)
                    .HasColumnName("kind")
                    .IsRequired();

            builder.Property(t => t.Order)
                    .HasColumnName("order")
                    .HasDefaultValue(0)
                    .IsRequired();

            builder.HasIndex(t => t.Name)
                    .IsUnique()
                    .HasDatabaseName("ix_time_periods_name");
        }
    }
}
