using Infrastructure.Persistence.EntityConfigurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Identity.EntityConfiguration
{
    public class ApplicationRoleConfiguration : AuditableEntityConfiguration<ApplicationRole>
    {
        public override void Configure(EntityTypeBuilder<ApplicationRole> builder)
        {
            base.Configure(builder);

            builder.ToTable("application_roles", schema: "identity");

            builder.HasKey(r => r.Id);

            builder.Property(r => r.Id)
                    .IsRequired()
                    .HasColumnName("id");

            builder.Property(r => r.Name)
                    .HasColumnName("name")
                    .HasMaxLength(256);

            builder.Property(r => r.NormalizedName)
                    .HasColumnName("normalized_name")
                    .HasMaxLength(256);

            builder.Property(r => r.ConcurrencyStamp)
                    .HasColumnName("concurrency_stamp");

            builder.Property(r => r.Description)
                    .HasColumnName("description")
                    .HasMaxLength(500);

            builder.HasIndex(r => r.NormalizedName)
                    .IsUnique()
                    .HasDatabaseName("ix_identity_roles_normalized_name");
        }
    }
}
