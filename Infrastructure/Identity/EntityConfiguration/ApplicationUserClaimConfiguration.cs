using Infrastructure.Persistence.EntityConfigurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Identity.EntityConfiguration
{
    public class ApplicationUserClaimConfiguration : AuditableEntityConfiguration<ApplicationUserClaim>
    {
        public override void Configure(EntityTypeBuilder<ApplicationUserClaim> builder)
        {
            base.Configure(builder);

            builder.ToTable("application_user_claims", schema: "identity");

            builder.HasKey(uc => uc.Id);

            builder.Property(uc => uc.Id)
                    .IsRequired()
                    .HasColumnName("id");

            builder.Property(uc => uc.UserId)
                    .HasColumnName("user_id")
                    .IsRequired();

            builder.Property(uc => uc.MenuId)
                    .HasColumnName("menu_id")
                    .IsRequired();

            builder.Property(uc => uc.ClaimType)
                    .HasColumnName("claim_type");

            builder.Property(uc => uc.ClaimValue)
                    .HasColumnName("claim_value");

            builder.HasOne<ApplicationUser>()
                    .WithMany()
                    .HasForeignKey(uc => uc.UserId)
                    .IsRequired();

            builder.HasOne(uc => uc.Menu)
                    .WithMany()
                    .HasForeignKey(uc => uc.MenuId)
                    .IsRequired();

            builder.HasIndex(uc => uc.UserId)
                    .HasDatabaseName("ix_identity_user_claims_user_id");
        }
    }
}
