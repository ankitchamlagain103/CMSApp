using Domain.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations
{
    public class AuditableEntityConfiguration<T> : IEntityTypeConfiguration<T> where T : class, IAuditableEntity
    {
        public virtual void Configure(EntityTypeBuilder<T> builder)
        {
            builder.Property(u => u.CreatedTs)
                    .HasColumnName("created_ts")
                    .IsRequired();

            builder.Property(u => u.CreatedBy)
                   .HasColumnName("created_by")
                   .HasMaxLength(50)
                   .IsRequired();

            builder.Property(u => u.UpdatedTs)
                  .HasColumnName("updated_ts")
                  .IsRequired(false);

            builder.Property(u => u.UpdatedBy)
                 .HasColumnName("updated_by")
                 .HasMaxLength(50);
        }
    }
}
