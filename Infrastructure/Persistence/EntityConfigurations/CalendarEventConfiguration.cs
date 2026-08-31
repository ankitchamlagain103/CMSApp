using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations
{
    public class CalendarEventConfiguration : SoftDeleteAuditableEntityConfiguration<CalendarEvent>
    {
        public override void Configure(EntityTypeBuilder<CalendarEvent> builder)
        {
            base.Configure(builder);

            builder.ToTable("calendar_events", "dbo");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Id)
                    .HasColumnName("id");

            builder.Property(e => e.Title)
                    .HasColumnName("title")
                    .IsRequired()
                    .HasMaxLength(200);

            builder.Property(e => e.EventType)
                    .HasColumnName("event_type")
                    .IsRequired();

            // Calendar date with no time-of-day meaning -- plain 'date' column, same as
            // FiscalYear/Employee dates.
            builder.Property(e => e.AdDate)
                    .HasColumnName("ad_date")
                    .HasColumnType("date")
                    .IsRequired();

            builder.Property(e => e.BsYear)
                    .HasColumnName("bs_year")
                    .IsRequired();

            builder.Property(e => e.BsMonth)
                    .HasColumnName("bs_month")
                    .IsRequired();

            builder.Property(e => e.BsDay)
                    .HasColumnName("bs_day")
                    .IsRequired();

            builder.Property(e => e.Description)
                    .HasColumnName("description")
                    .HasMaxLength(1000);

            builder.Property(e => e.IconKey)
                    .HasColumnName("icon_key")
                    .HasMaxLength(100);

            builder.Property(e => e.ColorCode)
                    .HasColumnName("color_code")
                    .HasMaxLength(20);

            builder.Property(e => e.Language)
                    .HasColumnName("language")
                    .HasMaxLength(10);

            builder.Property(e => e.IsActive)
                    .HasColumnName("is_active")
                    .HasDefaultValue(true);

            // Config codes (ConfigTypeCodes.Province/Branch), not database FKs -- same convention
            // as GradeCode/SubjectCode. Null on both means "applies everywhere".
            builder.Property(e => e.ProvinceCode)
                    .HasColumnName("province_code")
                    .HasMaxLength(100);

            builder.Property(e => e.BranchCode)
                    .HasColumnName("branch_code")
                    .HasMaxLength(100);

            builder.Property(e => e.StudentId)
                    .HasColumnName("student_id");

            builder.Property(e => e.EmployeeId)
                    .HasColumnName("employee_id");

            builder.HasOne(e => e.Student)
                    .WithMany()
                    .HasForeignKey(e => e.StudentId)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.Employee)
                    .WithMany()
                    .HasForeignKey(e => e.EmployeeId)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(e => e.AdDate)
                    .HasDatabaseName("ix_calendar_events_ad_date");

            builder.HasIndex(e => new { e.BsYear, e.BsMonth, e.BsDay })
                    .HasDatabaseName("ix_calendar_events_bs_date");

            builder.HasIndex(e => e.StudentId)
                    .HasDatabaseName("ix_calendar_events_student_id");

            builder.HasIndex(e => e.EmployeeId)
                    .HasDatabaseName("ix_calendar_events_employee_id");
        }
    }
}
