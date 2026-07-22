using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations
{
    public class BsMonthLengthConfiguration : AuditableEntityConfiguration<BsMonthLength>
    {
        public override void Configure(EntityTypeBuilder<BsMonthLength> builder)
        {
            base.Configure(builder);

            builder.ToTable("bs_month_lengths", "dbo");

            builder.HasKey(m => m.Id);

            builder.Property(m => m.Id)
                    .HasColumnName("id");

            builder.Property(m => m.BsYear)
                    .HasColumnName("bs_year")
                    .IsRequired();

            builder.Property(m => m.BsMonth)
                    .HasColumnName("bs_month")
                    .IsRequired();

            builder.Property(m => m.DaysInMonth)
                    .HasColumnName("days_in_month")
                    .IsRequired();

            builder.HasIndex(m => new { m.BsYear, m.BsMonth })
                    .IsUnique()
                    .HasDatabaseName("ix_bs_month_lengths_year_month");
        }
    }
}
