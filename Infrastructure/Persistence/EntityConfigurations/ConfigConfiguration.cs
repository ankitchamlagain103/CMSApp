using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations
{
    public class ConfigConfiguration : AuditableEntityConfiguration<Config>
    {
        public override void Configure(EntityTypeBuilder<Config> builder)
        {
            base.Configure(builder);

            builder.ToTable("configs", "dbo");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.Id)
                    .IsRequired()
                    .HasColumnName("id");

            builder.Property(c => c.TypeCode)
                    .HasColumnName("type_code")
                    .IsRequired();

            builder.Property(c => c.Code)
                    .HasColumnName("code")
                    .IsRequired()
                    .HasMaxLength(100);

            builder.Property(c => c.Label)
                    .HasColumnName("label")
                    .IsRequired()
                    .HasMaxLength(256);

            builder.Property(c => c.Order)
                    .HasColumnName("order")
                    .IsRequired();

            builder.Property(c => c.AdditionalValue1)
                    .HasColumnName("additional_value1")
                    .HasMaxLength(500);

            builder.Property(c => c.AdditionalValue2)
                    .HasColumnName("additional_value2")
                    .HasMaxLength(500);

            builder.Property(c => c.AdditionalValue3)
                    .HasColumnName("additional_value3")
                    .HasMaxLength(500);

            // The FK targets ConfigType.TypeCode (an alternate key), not the Guid primary key --
            // without HasPrincipalKey EF would point type_code at config_types.id and fail, or
            // invent a shadow ConfigTypeId column.
            builder.HasOne(c => c.ConfigType)
                    .WithMany(ct => ct.Configs)
                    .HasForeignKey(c => c.TypeCode)
                    .HasPrincipalKey(ct => ct.TypeCode)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(c => new { c.TypeCode, c.Code })
                    .IsUnique()
                    .HasDatabaseName("ix_configs_type_code_code");
        }
    }
}
