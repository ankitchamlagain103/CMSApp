using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations
{
    public class LeaveRequestConfiguration : SoftDeleteAuditableEntityConfiguration<LeaveRequest>
    {
        public override void Configure(EntityTypeBuilder<LeaveRequest> builder)
        {
            base.Configure(builder);

            builder.ToTable("leave_requests", "dbo");

            builder.HasKey(r => r.Id);

            builder.Property(r => r.Id)
                    .HasColumnName("id");

            builder.Property(r => r.EmployeeId)
                    .HasColumnName("employee_id")
                    .IsRequired();

            builder.Property(r => r.LeaveTypeId)
                    .HasColumnName("leave_type_id")
                    .IsRequired();

            builder.Property(r => r.FromDate)
                    .HasColumnName("from_date")
                    .HasColumnType("date")
                    .IsRequired();

            builder.Property(r => r.ToDate)
                    .HasColumnName("to_date")
                    .HasColumnType("date")
                    .IsRequired();

            builder.Property(r => r.Days)
                    .HasColumnName("days")
                    .HasColumnType("decimal(6,2)")
                    .IsRequired();

            builder.Property(r => r.Reason)
                    .HasColumnName("reason")
                    .HasMaxLength(1000);

            builder.Property(r => r.SubstituteEmployeeId)
                    .HasColumnName("substitute_employee_id");

            builder.Property(r => r.IsEmergency)
                    .HasColumnName("is_emergency")
                    .IsRequired();

            builder.Property(r => r.ManagerStatus)
                    .HasColumnName("manager_status")
                    .IsRequired();

            builder.Property(r => r.ManagerRemarks)
                    .HasColumnName("manager_remarks")
                    .HasMaxLength(500);

            builder.Property(r => r.ManagerDecisionTs)
                    .HasColumnName("manager_decision_ts");

            builder.Property(r => r.ManagerDecisionBy)
                    .HasColumnName("manager_decision_by")
                    .HasMaxLength(256);

            builder.Property(r => r.HrStatus)
                    .HasColumnName("hr_status")
                    .IsRequired();

            builder.Property(r => r.HrRemarks)
                    .HasColumnName("hr_remarks")
                    .HasMaxLength(500);

            builder.Property(r => r.HrDecisionTs)
                    .HasColumnName("hr_decision_ts");

            builder.Property(r => r.HrDecisionBy)
                    .HasColumnName("hr_decision_by")
                    .HasMaxLength(256);

            builder.Property(r => r.AttachmentPath)
                    .HasColumnName("attachment_path")
                    .HasMaxLength(500);

            builder.Property(r => r.AttachmentFileName)
                    .HasColumnName("attachment_file_name")
                    .HasMaxLength(255);

            builder.Property(r => r.AttachmentContentType)
                    .HasColumnName("attachment_content_type")
                    .HasMaxLength(100);

            builder.HasOne(r => r.Employee)
                    .WithMany(e => e.LeaveRequests)
                    .HasForeignKey(r => r.EmployeeId)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(r => r.LeaveType)
                    .WithMany()
                    .HasForeignKey(r => r.LeaveTypeId)
                    .OnDelete(DeleteBehavior.Restrict);

            // Explicit FK: the nav is named SubstituteEmployee, not "Employee" + "Id", so EF's
            // convention wouldn't find SubstituteEmployeeId on its own -- same caution
            // RefreshToken.UserId documents. No back-collection on Employee (WithMany()) --
            // "requests I'm covering for" isn't a lookup this codebase needs from the Employee
            // side today.
            builder.HasOne(r => r.SubstituteEmployee)
                    .WithMany()
                    .HasForeignKey(r => r.SubstituteEmployeeId)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(r => r.EmployeeId)
                    .HasDatabaseName("ix_leave_requests_employee_id");

            builder.HasIndex(r => new { r.FromDate, r.ToDate })
                    .HasDatabaseName("ix_leave_requests_date_range");
        }
    }
}
