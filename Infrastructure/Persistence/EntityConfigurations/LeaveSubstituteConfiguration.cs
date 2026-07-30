using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations
{
    public class LeaveSubstituteConfiguration : AuditableEntityConfiguration<LeaveSubstitute>
    {
        public override void Configure(EntityTypeBuilder<LeaveSubstitute> builder)
        {
            base.Configure(builder);

            builder.ToTable("leave_substitutes", "dbo");

            builder.HasKey(s => s.Id);

            builder.Property(s => s.Id)
                    .HasColumnName("id");

            builder.Property(s => s.LeaveRequestId)
                    .HasColumnName("leave_request_id")
                    .IsRequired();

            builder.Property(s => s.EmployeeId)
                    .HasColumnName("employee_id")
                    .IsRequired();

            builder.Property(s => s.Responsibility)
                    .HasColumnName("responsibility")
                    .HasMaxLength(255);

            builder.HasOne(s => s.LeaveRequest)
                    .WithMany(r => r.Substitutes)
                    .HasForeignKey(s => s.LeaveRequestId)
                    .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(s => s.Employee)
                    .WithMany()
                    .HasForeignKey(s => s.EmployeeId)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(s => s.LeaveRequestId)
                    .HasDatabaseName("ix_leave_substitutes_leave_request_id");
        }
    }
}
