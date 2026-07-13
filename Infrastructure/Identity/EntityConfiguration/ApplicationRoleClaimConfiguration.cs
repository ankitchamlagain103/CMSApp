using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Identity.EntityConfiguration
{
    public class ApplicationRoleClaimConfiguration : IEntityTypeConfiguration<ApplicationRoleClaim>
    {
        public void Configure(EntityTypeBuilder<ApplicationRoleClaim> builder)
        {
            builder.ToTable("application_role_claims", schema: "identity");

            builder.HasKey(rc => rc.Id);

            builder.Property(rc => rc.Id)
                    .IsRequired()
                    .HasColumnName("id");

            builder.Property(rc => rc.RoleId)
                    .HasColumnName("role_id")
                    .IsRequired();

            builder.Property(rc => rc.MenuId)
                    .HasColumnName("menu_id")
                    .IsRequired();

            builder.Property(rc => rc.ClaimType)
                    .HasColumnName("claim_type");

            builder.Property(rc => rc.ClaimValue)
                    .HasColumnName("claim_value");

            builder.HasOne(rc => rc.ApplicationRole)
                    .WithMany(r => r.ApplicationRoleClaims)
                    .HasForeignKey(rc => rc.RoleId)
                    .IsRequired();

            builder.HasOne(rc => rc.Menu)
                    .WithMany()
                    .HasForeignKey(rc => rc.MenuId)
                    .IsRequired();

            builder.HasIndex(rc => rc.RoleId)
                    .HasDatabaseName("ix_identity_role_claims_role_id");
        }
    }
}
