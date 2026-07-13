using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Identity.EntityConfiguration
{
    public class ApplicationUserTokenConfiguration : IEntityTypeConfiguration<ApplicationUserToken>
    {
        public void Configure(EntityTypeBuilder<ApplicationUserToken> builder)
        {
            builder.ToTable("application_user_tokens", schema: "identity");

            builder.HasKey(t => new { t.UserId, t.LoginProvider, t.Name });

            builder.Property(t => t.UserId)
                    .HasColumnName("user_id");

            builder.Property(t => t.LoginProvider)
                    .HasColumnName("login_provider")
                    .HasMaxLength(128);

            builder.Property(t => t.Name)
                    .HasColumnName("name")
                    .HasMaxLength(128);

            builder.Property(t => t.Value)
                    .HasColumnName("value");

            builder.Property(t => t.RefreshTokenExpiryDate)
                    .HasColumnName("refresh_token_expiry_date")
                    .IsRequired();

            builder.Property(t => t.TokenRefreshTokenRevokedDate)
                    .HasColumnName("token_refresh_token_revoked_date");

            builder.HasOne(t => t.ApplicationUser)
                    .WithMany(u => u.ApplicationUserTokens)
                    .HasForeignKey(t => t.UserId)
                    .IsRequired();
        }
    }
}
