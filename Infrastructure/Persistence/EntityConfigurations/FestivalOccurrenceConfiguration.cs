using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations
{
    public class FestivalOccurrenceConfiguration : SoftDeleteAuditableEntityConfiguration<FestivalOccurrence>
    {
        public override void Configure(EntityTypeBuilder<FestivalOccurrence> builder)
        {
            base.Configure(builder);

            builder.ToTable("festival_occurrences", "dbo");

            builder.HasKey(f => f.Id);

            builder.Property(f => f.Id)
                    .HasColumnName("id");

            builder.Property(f => f.FestivalName)
                    .HasColumnName("festival_name")
                    .IsRequired()
                    .HasMaxLength(200);

            builder.Property(f => f.Category)
                    .HasColumnName("category")
                    .IsRequired();

            builder.Property(f => f.BsYear)
                    .HasColumnName("bs_year")
                    .IsRequired();

            builder.Property(f => f.BsStartMonth)
                    .HasColumnName("bs_start_month")
                    .IsRequired();

            builder.Property(f => f.BsStartDay)
                    .HasColumnName("bs_start_day")
                    .IsRequired();

            builder.Property(f => f.BsEndMonth)
                    .HasColumnName("bs_end_month")
                    .IsRequired();

            builder.Property(f => f.BsEndDay)
                    .HasColumnName("bs_end_day")
                    .IsRequired();

            builder.Property(f => f.AdStartDate)
                    .HasColumnName("ad_start_date")
                    .HasColumnType("date")
                    .IsRequired();

            builder.Property(f => f.AdEndDate)
                    .HasColumnName("ad_end_date")
                    .HasColumnType("date")
                    .IsRequired();

            builder.Property(f => f.Description)
                    .HasColumnName("description")
                    .HasMaxLength(1000);

            builder.Property(f => f.ColorCode)
                    .HasColumnName("color_code")
                    .HasMaxLength(20);

            builder.Property(f => f.IsActive)
                    .HasColumnName("is_active")
                    .HasDefaultValue(true);

            builder.HasIndex(f => new { f.AdStartDate, f.AdEndDate })
                    .HasDatabaseName("ix_festival_occurrences_ad_range");

            builder.HasIndex(f => f.BsYear)
                    .HasDatabaseName("ix_festival_occurrences_bs_year");
        }
    }
}
