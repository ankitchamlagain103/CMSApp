using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations
{
    public class GuardianConfiguration : SoftDeleteAuditableEntityConfiguration<Guardian>
    {
        public override void Configure(EntityTypeBuilder<Guardian> builder)
        {
            base.Configure(builder);

            builder.ToTable("guardians", "dbo");

            builder.HasKey(g => g.Id);

            builder.Property(g => g.Id)
                    .HasColumnName("id");

            builder.Property(g => g.FirstName)
                    .HasColumnName("first_name")
                    .IsRequired()
                    .HasMaxLength(100);

            builder.Property(g => g.LastName)
                    .HasColumnName("last_name")
                    .IsRequired()
                    .HasMaxLength(100);

            builder.Property(g => g.Email)
                    .HasColumnName("email")
                    .HasMaxLength(255);

            builder.Property(g => g.Phone)
                    .HasColumnName("phone")
                    .HasMaxLength(20);

            builder.Property(g => g.Occupation)
                    .HasColumnName("occupation")
                    .HasMaxLength(150);

            builder.Property(g => g.Address)
                    .HasColumnName("address")
                    .HasMaxLength(500);
        }
    }
}
