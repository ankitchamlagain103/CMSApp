using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations
{
    public class ClassTimePeriodConfiguration : AuditableEntityConfiguration<ClassTimePeriod>
    {
        public override void Configure(EntityTypeBuilder<ClassTimePeriod> builder)
        {
            base.Configure(builder);

            builder.ToTable("class_time_periods", "dbo");

            builder.HasKey(m => m.Id);

            builder.Property(m => m.Id)
                    .HasColumnName("id");

            builder.Property(m => m.AcademicClassId)
                    .HasColumnName("academic_class_id")
                    .IsRequired();

            builder.Property(m => m.TimePeriodId)
                    .HasColumnName("time_period_id")
                    .IsRequired();

            builder.HasOne(m => m.AcademicClass)
                    .WithMany()
                    .HasForeignKey(m => m.AcademicClassId)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(m => m.TimePeriod)
                    .WithMany()
                    .HasForeignKey(m => m.TimePeriodId)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(m => new { m.AcademicClassId, m.TimePeriodId })
                    .IsUnique()
                    .HasDatabaseName("ix_class_time_periods_class_period");
        }
    }
}
