using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations
{
    public class StudentDocumentConfiguration : AuditableEntityConfiguration<StudentDocument>
    {
        public override void Configure(EntityTypeBuilder<StudentDocument> builder)
        {
            base.Configure(builder);

            builder.ToTable("student_documents", "dbo");

            builder.HasKey(d => d.Id);

            builder.Property(d => d.Id)
                    .HasColumnName("id");

            builder.Property(d => d.StudentId)
                    .HasColumnName("student_id")
                    .IsRequired();

            // Config code (TypeCode 1007), not a database FK -- see AcademicClassConfiguration.
            builder.Property(d => d.DocumentTypeCode)
                    .HasColumnName("document_type_code")
                    .IsRequired()
                    .HasMaxLength(100);

            builder.Property(d => d.DocumentName)
                    .HasColumnName("document_name")
                    .IsRequired()
                    .HasMaxLength(150);

            builder.Property(d => d.FileName)
                    .HasColumnName("file_name")
                    .IsRequired()
                    .HasMaxLength(255);

            builder.Property(d => d.FilePath)
                    .HasColumnName("file_path")
                    .IsRequired()
                    .HasMaxLength(500);

            builder.Property(d => d.ContentType)
                    .HasColumnName("content_type")
                    .HasMaxLength(100);

            builder.Property(d => d.FileSizeBytes)
                    .HasColumnName("file_size_bytes")
                    .IsRequired();

            builder.Property(d => d.ValidUntil)
                    .HasColumnName("valid_until")
                    .HasColumnType("date");

            builder.Property(d => d.Remarks)
                    .HasColumnName("remarks")
                    .HasMaxLength(500);

            builder.HasOne(d => d.Student)
                    .WithMany(s => s.Documents)
                    .HasForeignKey(d => d.StudentId)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(d => d.StudentId)
                    .HasDatabaseName("ix_student_documents_student_id");
        }
    }
}
