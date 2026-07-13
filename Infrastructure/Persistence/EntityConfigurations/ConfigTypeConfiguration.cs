using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations
{
    public class ConfigTypeConfiguration : AuditableEntityConfiguration<ConfigType>
    {
        public override void Configure(EntityTypeBuilder<ConfigType> builder)
        {
            base.Configure(builder);

            builder.ToTable("config_types", "dbo");

            builder.HasKey(ct => ct.Id);

            builder.Property(ct => ct.Id)
                    .IsRequired()
                    .HasColumnName("id");

            builder.Property(ct => ct.TypeCode)
                    .HasColumnName("type_code")
                    .IsRequired();

            builder.Property(ct => ct.Name)
                    .HasColumnName("name")
                    .IsRequired()
                    .HasMaxLength(100);

            builder.Property(ct => ct.Description)
                    .HasColumnName("description")
                    .HasMaxLength(500);

            builder.HasIndex(ct => ct.TypeCode)
                    .IsUnique()
                    .HasDatabaseName("ix_config_types_type_code");
        }
    }
}
