using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations
{
    public class FeeRuleConfiguration : SoftDeleteAuditableEntityConfiguration<FeeRule>
    {
        public override void Configure(EntityTypeBuilder<FeeRule> builder)
        {
            base.Configure(builder);

            builder.ToTable("fee_rules", "dbo");

            builder.HasKey(r => r.Id);

            builder.Property(r => r.Id)
                    .HasColumnName("id");

            builder.Property(r => r.Code)
                    .HasColumnName("code")
                    .IsRequired()
                    .HasMaxLength(100);

            builder.Property(r => r.Name)
                    .HasColumnName("name")
                    .IsRequired()
                    .HasMaxLength(200);

            builder.Property(r => r.RuleType)
                    .HasColumnName("rule_type")
                    .IsRequired();

            builder.Property(r => r.TriggerStage)
                    .HasColumnName("trigger_stage")
                    .IsRequired();

            builder.Property(r => r.ValueType)
                    .HasColumnName("value_type")
                    .IsRequired();

            builder.Property(r => r.Value)
                    .HasColumnName("value")
                    .HasColumnType("decimal(10,2)")
                    .IsRequired();

            builder.Property(r => r.MinMonthsTogether)
                    .HasColumnName("min_months_together");

            builder.Property(r => r.DaysBeforeDueDate)
                    .HasColumnName("days_before_due_date");

            builder.Property(r => r.AcademicClassId)
                    .HasColumnName("academic_class_id");

            // Config code (TypeCode ConfigTypeCodes.FeeCategory), not a database FK.
            builder.Property(r => r.FeeCategoryCode)
                    .HasColumnName("fee_category_code")
                    .HasMaxLength(100);

            builder.Property(r => r.EffectiveFrom)
                    .HasColumnName("effective_from")
                    .HasColumnType("date")
                    .IsRequired();

            builder.Property(r => r.EffectiveTo)
                    .HasColumnName("effective_to")
                    .HasColumnType("date");

            builder.Property(r => r.Priority)
                    .HasColumnName("priority")
                    .IsRequired();

            builder.Property(r => r.IsCombinable)
                    .HasColumnName("is_combinable")
                    .HasDefaultValue(false);

            builder.Property(r => r.IsActive)
                    .HasColumnName("is_active")
                    .HasDefaultValue(true);

            builder.HasOne(r => r.AcademicClass)
                    .WithMany()
                    .HasForeignKey(r => r.AcademicClassId)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(r => r.Code)
                    .IsUnique()
                    .HasDatabaseName("ix_fee_rules_code");
        }
    }
}
