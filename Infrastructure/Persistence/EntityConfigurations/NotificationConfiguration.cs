using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations
{
    public class NotificationConfiguration : AuditableEntityConfiguration<Notification>
    {
        public override void Configure(EntityTypeBuilder<Notification> builder)
        {
            base.Configure(builder);

            builder.ToTable("notifications", "dbo");

            builder.HasKey(n => n.Id);

            builder.Property(n => n.Id)
                    .HasColumnName("id");

            builder.Property(n => n.EmployeeId)
                    .HasColumnName("employee_id")
                    .IsRequired();

            builder.Property(n => n.Title)
                    .HasColumnName("title")
                    .IsRequired()
                    .HasMaxLength(200);

            builder.Property(n => n.Message)
                    .HasColumnName("message")
                    .IsRequired()
                    .HasMaxLength(1000);

            builder.Property(n => n.Type)
                    .HasColumnName("type")
                    .IsRequired();

            builder.Property(n => n.IsRead)
                    .HasColumnName("is_read")
                    .IsRequired()
                    .HasDefaultValue(false);

            builder.HasOne(n => n.Employee)
                    .WithMany()
                    .HasForeignKey(n => n.EmployeeId)
                    .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(n => new { n.EmployeeId, n.IsRead })
                    .HasDatabaseName("ix_notifications_employee_id_is_read");
        }
    }
}
