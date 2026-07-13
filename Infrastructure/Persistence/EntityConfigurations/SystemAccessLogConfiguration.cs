using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations
{
    public class SystemAccessLogConfiguration : AuditableEntityConfiguration<SystemAccessLog>
    {
        public override void Configure(EntityTypeBuilder<SystemAccessLog> builder)
        {
            base.Configure(builder);

            builder.ToTable("system_access_logs", "dbo");

            builder.HasKey(log => log.Id);

            builder.Property(log => log.Id)
                    .IsRequired()
                    .HasColumnName("id");

            builder.Property(log => log.UserId)
                    .HasColumnName("user_id")
                    .IsRequired();

            builder.Property(log => log.UserName)
                    .HasColumnName("user_name")
                    .HasMaxLength(256);

            builder.Property(log => log.Controller)
                    .HasColumnName("controller")
                    .IsRequired()
                    .HasMaxLength(100);

            builder.Property(log => log.Action)
                    .HasColumnName("action")
                    .IsRequired()
                    .HasMaxLength(100);

            builder.Property(log => log.HttpMethod)
                    .HasColumnName("http_method")
                    .HasMaxLength(10);

            builder.Property(log => log.Url)
                    .HasColumnName("url")
                    .HasMaxLength(2000);

            builder.Property(log => log.IpAddress)
                    .HasColumnName("ip_address")
                    .HasMaxLength(45);

            builder.HasIndex(log => log.UserId)
                    .HasDatabaseName("ix_system_access_logs_user_id");
        }
    }
}
