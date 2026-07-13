using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations
{
    public class EnrollmentSubjectConfiguration : AuditableEntityConfiguration<EnrollmentSubject>
    {
        public override void Configure(EntityTypeBuilder<EnrollmentSubject> builder)
        {
            base.Configure(builder);

            builder.ToTable("enrollment_subjects", "dbo");

            builder.HasKey(es => es.Id);

            builder.Property(es => es.Id)
                    .HasColumnName("id");

            builder.Property(es => es.EnrollmentId)
                    .HasColumnName("enrollment_id")
                    .IsRequired();

            builder.Property(es => es.ClassSubjectId)
                    .HasColumnName("class_subject_id")
                    .IsRequired();

            builder.HasOne(es => es.Enrollment)
                    .WithMany(e => e.ElectiveSubjects)
                    .HasForeignKey(es => es.EnrollmentId)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(es => es.ClassSubject)
                    .WithMany()
                    .HasForeignKey(es => es.ClassSubjectId)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(es => new { es.EnrollmentId, es.ClassSubjectId })
                    .IsUnique()
                    .HasDatabaseName("ix_enrollment_subjects_enrollment_class_subject");
        }
    }
}
