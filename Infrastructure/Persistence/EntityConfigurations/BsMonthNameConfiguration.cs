using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations
{
    public class BsMonthNameConfiguration : AuditableEntityConfiguration<BsMonthName>
    {
        public override void Configure(EntityTypeBuilder<BsMonthName> builder)
        {
            base.Configure(builder);

            builder.ToTable("bs_month_names", "dbo");

            builder.HasKey(m => m.Id);

            builder.Property(m => m.Id)
                    .HasColumnName("id");

            builder.Property(m => m.MonthNumber)
                    .HasColumnName("month_number")
                    .IsRequired();

            builder.Property(m => m.NameEn)
                    .HasColumnName("name_en")
                    .IsRequired()
                    .HasMaxLength(50);

            builder.Property(m => m.NameNp)
                    .HasColumnName("name_np")
                    .IsRequired()
                    .HasMaxLength(50);

            builder.HasIndex(m => m.MonthNumber)
                    .IsUnique()
                    .HasDatabaseName("ix_bs_month_names_month_number");
        }
    }
}
