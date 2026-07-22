using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations
{
    public class MeetingConfiguration : SoftDeleteAuditableEntityConfiguration<Meeting>
    {
        public override void Configure(EntityTypeBuilder<Meeting> builder)
        {
            base.Configure(builder);

            builder.ToTable("meetings", "dbo");

            builder.HasKey(m => m.Id);

            builder.Property(m => m.Id)
                    .HasColumnName("id");

            builder.Property(m => m.Title)
                    .HasColumnName("title")
                    .IsRequired()
                    .HasMaxLength(250);

            builder.Property(m => m.Description)
                    .HasColumnName("description")
                    .HasMaxLength(2000);

            builder.Property(m => m.AdDate)
                    .HasColumnName("ad_date")
                    .HasColumnType("date")
                    .IsRequired();

            // Wall-clock times within AdDate -- 'time' columns (Npgsql maps TimeSpan to
            // 'time' when told to; the default would be 'interval').
            builder.Property(m => m.StartTime)
                    .HasColumnName("start_time")
                    .HasColumnType("time")
                    .IsRequired();

            builder.Property(m => m.EndTime)
                    .HasColumnName("end_time")
                    .HasColumnType("time")
                    .IsRequired();

            builder.Property(m => m.BsYear)
                    .HasColumnName("bs_year")
                    .IsRequired();

            builder.Property(m => m.BsMonth)
                    .HasColumnName("bs_month")
                    .IsRequired();

            builder.Property(m => m.BsDay)
                    .HasColumnName("bs_day")
                    .IsRequired();

            builder.Property(m => m.IsVirtual)
                    .HasColumnName("is_virtual")
                    .HasDefaultValue(false);

            builder.Property(m => m.Location)
                    .HasColumnName("location")
                    .HasMaxLength(500);

            // Identity user id as a plain scalar -- no FK/navigation across the
            // Domain/Identity boundary.
            builder.Property(m => m.HostUserId)
                    .HasColumnName("host_user_id")
                    .IsRequired();

            builder.HasIndex(m => m.AdDate)
                    .HasDatabaseName("ix_meetings_ad_date");

            builder.HasIndex(m => new { m.BsYear, m.BsMonth, m.BsDay })
                    .HasDatabaseName("ix_meetings_bs_date");

            builder.HasIndex(m => m.HostUserId)
                    .HasDatabaseName("ix_meetings_host_user_id");
        }
    }
}
