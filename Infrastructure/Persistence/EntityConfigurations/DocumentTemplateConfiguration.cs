using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations
{
    public class DocumentTemplateConfiguration : AuditableEntityConfiguration<DocumentTemplate>
    {
        public override void Configure(EntityTypeBuilder<DocumentTemplate> builder)
        {
            base.Configure(builder);

            builder.ToTable("document_templates", "dbo");

            builder.HasKey(t => t.Id);

            builder.Property(t => t.Id)
                   .IsRequired()
                   .HasColumnName("id");

            builder.Property(t => t.TemplateType)
                   .HasColumnName("template_type")
                   .IsRequired();

            builder.Property(t => t.Name)
                   .HasColumnName("name")
                   .IsRequired()
                   .HasMaxLength(256);

            builder.Property(t => t.HtmlContent)
                   .HasColumnName("html_content")
                   .IsRequired()
                   .HasColumnType("text");

            builder.HasIndex(t => t.TemplateType)
                   .IsUnique()
                   .HasDatabaseName("ix_document_templates_template_type");
        }
    }
}
