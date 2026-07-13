using Domain.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations
{
    public class SoftDeleteAuditableEntityConfiguration<T> : IEntityTypeConfiguration<T> where T : class, ISoftDeleteAuditableEntity
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

            builder.Property(u => u.IsDeleted)
                .HasColumnName("is_deleted")
                .HasDefaultValue(false);

            builder.Property(u => u.DeletedTs)
                .HasColumnName("deleted_ts");

            builder.Property(u => u.DeletedBy)
                 .HasColumnName("deleted_by")
                 .HasMaxLength(50);

            // Global query filter for soft delete
            builder.HasQueryFilter(e => !e.IsDeleted);

            // Add index for performance
            builder.HasIndex(e => e.IsDeleted);
        }
    }
}
