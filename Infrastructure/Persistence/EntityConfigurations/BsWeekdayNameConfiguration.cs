using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations
{
    public class BsWeekdayNameConfiguration : AuditableEntityConfiguration<BsWeekdayName>
    {
        public override void Configure(EntityTypeBuilder<BsWeekdayName> builder)
        {
            base.Configure(builder);

            builder.ToTable("bs_weekday_names", "dbo");

            builder.HasKey(w => w.Id);

            builder.Property(w => w.Id)
                    .HasColumnName("id");

            builder.Property(w => w.WeekdayIndex)
                    .HasColumnName("weekday_index")
                    .IsRequired();

            builder.Property(w => w.NameEn)
                    .HasColumnName("name_en")
                    .IsRequired()
                    .HasMaxLength(50);

            builder.Property(w => w.NameNp)
                    .HasColumnName("name_np")
                    .IsRequired()
                    .HasMaxLength(50);

            builder.Property(w => w.IsWeeklyHoliday)
                    .HasColumnName("is_weekly_holiday")
                    .HasDefaultValue(false);

            builder.HasIndex(w => w.WeekdayIndex)
                    .IsUnique()
                    .HasDatabaseName("ix_bs_weekday_names_weekday_index");
        }
    }
}
