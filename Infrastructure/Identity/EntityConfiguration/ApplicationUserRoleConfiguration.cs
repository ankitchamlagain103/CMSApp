using Infrastructure.Persistence.EntityConfigurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Identity.EntityConfiguration
{
    public class ApplicationUserRoleConfiguration : AuditableEntityConfiguration<ApplicationUserRole>
    {
        public override void Configure(EntityTypeBuilder<ApplicationUserRole> builder)
        {
            base.Configure(builder);

            builder.ToTable("application_user_roles", schema: "identity");

            builder.HasKey(ur => new { ur.UserId, ur.RoleId });

            builder.Property(ur => ur.UserId)
                    .HasColumnName("user_id")
                    .IsRequired();

            builder.Property(ur => ur.RoleId)
                    .HasColumnName("role_id")
                    .IsRequired();

            builder.HasOne(ur => ur.ApplicationUser)
                    .WithMany(u => u.ApplicationUserRoles)
                    .HasForeignKey(ur => ur.UserId)
                    .IsRequired();

            builder.HasOne(ur => ur.ApplicationRole)
                    .WithMany(r => r.ApplicationUserRoles)
                    .HasForeignKey(ur => ur.RoleId)
                    .IsRequired();
        }
    }
}
