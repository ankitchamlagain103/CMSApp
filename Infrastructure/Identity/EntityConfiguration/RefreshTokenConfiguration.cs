using Infrastructure.Persistence.EntityConfigurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Identity.EntityConfiguration
{
    public class RefreshTokenConfiguration : AuditableEntityConfiguration<RefreshToken>
    {
        public override void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            base.Configure(builder);

            builder.ToTable("refresh_tokens", schema: "identity");

            builder.HasKey(rt => rt.Id);

            builder.Property(rt => rt.Id)
                    .IsRequired()
                    .HasColumnName("id");

            builder.Property(rt => rt.UserId)
                    .HasColumnName("user_id")
                    .IsRequired();

            builder.Property(rt => rt.Token)
                    .HasColumnName("token")
                    .IsRequired()
                    .HasMaxLength(200);

            builder.Property(rt => rt.ExpiresAtUtc)
                    .HasColumnName("expires_at_utc")
                    .IsRequired();

            builder.Property(rt => rt.RevokedAtUtc)
                    .HasColumnName("revoked_at_utc");

            builder.Property(rt => rt.ReplacedByToken)
                    .HasColumnName("replaced_by_token")
                    .HasMaxLength(200);

            builder.HasOne(rt => rt.ApplicationUser)
                    .WithMany(x => x.RefreshTokens)
                    .HasForeignKey(rt => rt.UserId)
                    .IsRequired();

            builder.HasIndex(rt => rt.Token)
                    .IsUnique()
                    .HasDatabaseName("ix_identity_refresh_tokens_token");

            builder.HasIndex(rt => rt.UserId)
                    .HasDatabaseName("ix_identity_refresh_tokens_user_id");
        }
    }
}
