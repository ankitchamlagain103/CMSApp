using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations
{
    public class AppConfigConfiguration : AuditableEntityConfiguration<AppConfig>
    {
        public override void Configure(EntityTypeBuilder<AppConfig> builder)
        {
            base.Configure(builder);

            builder.ToTable("app_configs", "dbo");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.Id)
                    .IsRequired()
                    .HasColumnName("id");

            builder.Property(c => c.ConfigParam)
                   .HasColumnName("config_param")
                   .IsRequired()
                   .HasMaxLength(256);

            builder.Property(c => c.ConfigValue)
                   .HasColumnName("config_value")
                   .IsRequired()
                   .HasMaxLength(555);

            builder.Property(c => c.ConfigGroup)
                 .HasColumnName("config_group")
                 .IsRequired(false)
                 .HasMaxLength(256);

            builder.Property(c => c.IsEnable)
                   .HasColumnName("is_enable")
                   .IsRequired();

            builder.HasIndex(c => c.ConfigParam)
                   .IsUnique()
                   .HasDatabaseName("ix_app_configs_config_param");
        }
    }
}
