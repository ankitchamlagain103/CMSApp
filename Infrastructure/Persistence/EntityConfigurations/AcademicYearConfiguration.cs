using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations
{
    public class AcademicYearConfiguration : SoftDeleteAuditableEntityConfiguration<AcademicYear>
    {
        public override void Configure(EntityTypeBuilder<AcademicYear> builder)
        {
            base.Configure(builder);

            builder.ToTable("academic_years", "dbo");

            builder.HasKey(y => y.Id);

            builder.Property(y => y.Id)
                    .HasColumnName("id");

            builder.Property(y => y.Code)
                    .HasColumnName("code")
                    .IsRequired()
                    .HasMaxLength(20);

            builder.Property(y => y.Name)
                    .HasColumnName("name")
                    .IsRequired()
                    .HasMaxLength(100);

            builder.Property(y => y.StartDate)
                    .HasColumnName("start_date")
                    .HasColumnType("date")
                    .IsRequired();

            builder.Property(y => y.EndDate)
                    .HasColumnName("end_date")
                    .HasColumnType("date")
                    .IsRequired();

            builder.Property(y => y.IsCurrent)
                    .HasColumnName("is_current")
                    .HasDefaultValue(false);

            builder.Property(y => y.Status)
                    .HasColumnName("status")
                    .IsRequired();

            builder.HasIndex(y => y.Code)
                    .IsUnique()
                    .HasDatabaseName("ix_academic_years_code");
        }
    }
}
