using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations
{
    public class MenuConfiguration : SoftDeleteAuditableEntityConfiguration<Menu>
    {
        public override void Configure(EntityTypeBuilder<Menu> builder)
        {
            base.Configure(builder);

            builder.ToTable("menus","dbo");

            builder.HasKey(m => m.Id);

            builder.Property(m => m.Id)
                    .IsRequired()
                    .HasColumnName("id");

            builder.Property(m => m.Code)
                    .HasColumnName("code")
                    .IsRequired()
                    .HasMaxLength(100);

            builder.Property(m => m.DisplayName)
                    .HasColumnName("display_name")
                    .IsRequired()
                    .HasMaxLength(256);

            builder.Property(m => m.Url)
                    .HasColumnName("url")
                    .HasMaxLength(500);

            builder.Property(m => m.Icon)
                    .HasColumnName("icon")
                    .HasMaxLength(100);

            builder.Property(m => m.MenuType)
                    .HasColumnName("menu_type")
                    .IsRequired()
                    .HasMaxLength(50);

            builder.Property(m => m.Controller)
                    .HasColumnName("controller")
                    .HasMaxLength(100);

            builder.Property(m => m.Action)
                    .HasColumnName("action")
                    .HasMaxLength(100);

            builder.Property(m => m.ParentId)
                    .HasColumnName("parent_id");

            builder.Property(m => m.MenuFor)
                    .HasColumnName("menu_for")
                    .IsRequired()
                    .HasMaxLength(50);

            builder.Property(m => m.Order)
                    .HasColumnName("order")
                    .IsRequired();

            builder.Property(m => m.IsHidden)
                    .HasColumnName("is_hidden")
                    .HasDefaultValue(false);

            builder.HasOne(m => m.MainMenu)
                    .WithMany(m => m.Childrens)
                    .HasForeignKey(m => m.ParentId)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(m => m.Code)
                    .IsUnique()
                    .HasDatabaseName("ix_menus_code");

            builder.HasIndex(m => m.ParentId)
                    .HasDatabaseName("ix_menus_parent_id");
        }
    }
}
