using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations
{
    public class StudentGuardianConfiguration : AuditableEntityConfiguration<StudentGuardian>
    {
        public override void Configure(EntityTypeBuilder<StudentGuardian> builder)
        {
            base.Configure(builder);

            builder.ToTable("student_guardians", "dbo");

            builder.HasKey(sg => sg.Id);

            builder.Property(sg => sg.Id)
                    .HasColumnName("id");

            builder.Property(sg => sg.StudentId)
                    .HasColumnName("student_id")
                    .IsRequired();

            builder.Property(sg => sg.GuardianId)
                    .HasColumnName("guardian_id")
                    .IsRequired();

            // Config code (TypeCode 1004), not a database FK.
            builder.Property(sg => sg.RelationshipCode)
                    .HasColumnName("relationship_code")
                    .IsRequired()
                    .HasMaxLength(100);

            builder.Property(sg => sg.IsPrimary)
                    .HasColumnName("is_primary")
                    .HasDefaultValue(false);

            builder.HasOne(sg => sg.Student)
                    .WithMany(s => s.GuardianLinks)
                    .HasForeignKey(sg => sg.StudentId)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(sg => sg.Guardian)
                    .WithMany(g => g.StudentLinks)
                    .HasForeignKey(sg => sg.GuardianId)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(sg => new { sg.StudentId, sg.GuardianId })
                    .IsUnique()
                    .HasDatabaseName("ix_student_guardians_student_guardian");
        }
    }
}
