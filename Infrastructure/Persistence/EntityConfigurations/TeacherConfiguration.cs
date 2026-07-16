using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations
{
    // Shared-primary-key 1:1 with Employee: Teacher.Id is always equal to its owning
    // Employee.Id (never independently generated) -- this is what lets
    // TeacherAssignment/TeacherDocument/TeacherQualification keep their TeacherId FK unchanged
    // across the Employee/Teacher split.
    public class TeacherConfiguration : AuditableEntityConfiguration<Teacher>
    {
        public override void Configure(EntityTypeBuilder<Teacher> builder)
        {
            base.Configure(builder);

            builder.ToTable("teachers", "dbo");

            builder.HasKey(t => t.Id);

            builder.Property(t => t.Id)
                    .HasColumnName("id")
                    .ValueGeneratedNever();

            builder.Property(t => t.TeachingLicenseNo)
                    .HasColumnName("teaching_license_no")
                    .HasMaxLength(100);

            builder.Property(t => t.ExperienceYears)
                    .HasColumnName("experience_years");

            builder.Property(t => t.Specialization)
                    .HasColumnName("specialization")
                    .HasMaxLength(255);

            builder.HasOne(t => t.Employee)
                    .WithOne(e => e.Teacher)
                    .HasForeignKey<Teacher>(t => t.Id)
                    .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
