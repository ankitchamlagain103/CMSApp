using Infrastructure.Persistence.EntityConfigurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Identity.EntityConfiguration
{
    public class ApplicationUserLoginConfiguration : AuditableEntityConfiguration<ApplicationUserLogin>
    {
        public override void Configure(EntityTypeBuilder<ApplicationUserLogin> builder)
        {
            base.Configure(builder);

            builder.ToTable("application_user_logins", schema: "identity");

            builder.HasKey(l => new { l.LoginProvider, l.ProviderKey });

            builder.Property(l => l.LoginProvider)
                    .HasColumnName("login_provider")
                    .HasMaxLength(128);

            builder.Property(l => l.ProviderKey)
                    .HasColumnName("provider_key")
                    .HasMaxLength(128);

            builder.Property(l => l.ProviderDisplayName)
                    .HasColumnName("provider_display_name");

            builder.Property(l => l.UserId)
                    .HasColumnName("user_id")
                    .IsRequired();

            builder.HasOne(l => l.ApplicationUser)
                    .WithMany(u => u.ApplicationUserLogins)
                    .HasForeignKey(l => l.UserId)
                    .IsRequired();
        }
    }
}
