using Infrastructure.Persistence.EntityConfigurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Identity.EntityConfiguration
{
    public class ApplicationUserConfiguration : SoftDeleteAuditableEntityConfiguration<ApplicationUser>
    {
        public override void Configure(EntityTypeBuilder<ApplicationUser> builder)
        {
            base.Configure(builder);

            builder.ToTable("application_users", schema: "identity");

            builder.HasKey(u => u.Id);

            builder.Property(u => u.Id)
                    .IsRequired()
                    .HasColumnName("id");

            builder.Property(u => u.UserType)
                    .HasColumnName("user_type")
                    .IsRequired(true);

            builder.Property(u => u.FirstName)
                   .HasColumnName("first_name")
                   .IsRequired()
                   .HasMaxLength(256);

            builder.Property(u => u.MiddleName)
                    .HasColumnName("middle_name")
                    .HasMaxLength(256);

            builder.Property(u => u.LastName)
                   .HasColumnName("last_name")
                   .IsRequired()
                   .HasMaxLength(256);


            builder.Property(u => u.UserName)
                    .HasColumnName("user_name")
                    .HasMaxLength(256)
                    .IsRequired(true);

            builder.Property(u => u.Dob)
                     .HasColumnName("dob")
                     .IsRequired(false);

            builder.Property(u => u.Gender)
                   .HasColumnName("gender")
                   .IsRequired(true);

            builder.Property(u => u.IsActive)
               .HasColumnName("is_active")
               .HasDefaultValue(true);

            builder.Property(u => u.IsTosAgreed)
               .HasColumnName("is_tos_agreed")
               .HasDefaultValue(false);

            builder.Property(u => u.IsIpRestricted)
               .HasColumnName("is_ip_restricted")
               .HasDefaultValue(false);

            builder.Property(u => u.UserIpAllowed)
               .HasColumnName("user_ip_allowed")
               .IsRequired(false);

            builder.Property(u => u.LastLoginTs)
                 .HasColumnName("last_login_ts");

            builder.Property(u => u.LastPasswordChangedTs)
                    .HasColumnName("last_password_changed_ts");

            builder.Property(u => u.NormalizedUserName)
                .HasColumnName("normalized_user_name")
                .HasMaxLength(256);

            builder.Property(u => u.Email)
                .HasColumnName("email")
                .HasMaxLength(320);

            builder.Property(u => u.NormalizedEmail)
                .HasColumnName("normalized_email")
                .HasMaxLength(320);

            builder.Property(u => u.EmailConfirmed)
                .HasColumnName("email_confirmed")
                .HasDefaultValue(false);

            builder.Property(u => u.CountryIso3)
                   .HasColumnName("country_iso3");

            builder.Property(u => u.PhoneCountryCode)
                    .HasColumnName("phone_country_code");

            builder.Property(u => u.PhoneNumber)
                .HasColumnName("phone_number")
                .HasMaxLength(20);

            builder.Property(u => u.PhoneNumberConfirmed)
                .HasColumnName("phone_number_confirmed")
                .HasDefaultValue(false);

            builder.Property(u => u.PasswordHash)
                .HasColumnName("password_hash");

            builder.Property(u => u.SecurityStamp)
                .HasColumnName("security_stamp");

            builder.Property(u => u.ConcurrencyStamp)
                .HasColumnName("concurrency_stamp");

            builder.Property(u => u.TwoFactorEnabled)
                .HasColumnName("two_factor_enabled")
                .HasDefaultValue(false);

            builder.Property(u => u.LockoutEnd)
                .HasColumnName("lockout_end");

            builder.Property(u => u.LockoutEnabled)
                .HasColumnName("lockout_enabled")
                .HasDefaultValue(true);

            builder.Property(u => u.AccessFailedCount)
                .HasColumnName("access_failed_count")
                .HasDefaultValue(0);

            builder.HasIndex(u => u.NormalizedEmail)
                .IsUnique()
                .HasDatabaseName("ix_identity_users_normalized_email");

            builder.HasIndex(u => u.NormalizedUserName)
                .IsUnique()
                .HasDatabaseName("ix_identity_users_normalized_user_name");
        }
    }
}
