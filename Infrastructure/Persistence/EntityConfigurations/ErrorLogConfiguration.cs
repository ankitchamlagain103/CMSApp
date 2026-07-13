using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations
{
    public class ErrorLogConfiguration : AuditableEntityConfiguration<ErrorLog>
    {
        public override void Configure(EntityTypeBuilder<ErrorLog> builder)
        {
            base.Configure(builder);

            builder.ToTable("error_logs", "dbo");

            builder.HasKey(log => log.Id);

            builder.Property(log => log.Id)
                    .IsRequired()
                    .HasColumnName("id");

            builder.Property(log => log.FingerprintHash)
                    .HasColumnName("fingerprint_hash")
                    .IsRequired()
                    .HasMaxLength(64);

            builder.Property(log => log.ExceptionType)
                    .HasColumnName("exception_type")
                    .HasMaxLength(512);

            builder.Property(log => log.Message)
                    .HasColumnName("message");

            builder.Property(log => log.StackTrace)
                    .HasColumnName("stack_trace");

            builder.Property(log => log.Path)
                    .HasColumnName("path")
                    .HasMaxLength(2000);

            builder.Property(log => log.ErrorCount)
                    .HasColumnName("error_count")
                    .IsRequired();

            builder.Property(log => log.LastOccurredTs)
                    .HasColumnName("last_occurred_ts")
                    .IsRequired();

            // Deliberately NOT unique: two requests hitting the same brand-new error at the same
            // moment could both insert before either sees the other's row. A duplicate row is a
            // cosmetic inconvenience; a unique-violation inside the error-logging path would
            // throw away the log entry entirely.
            builder.HasIndex(log => log.FingerprintHash)
                    .HasDatabaseName("ix_error_logs_fingerprint_hash");
        }
    }
}
