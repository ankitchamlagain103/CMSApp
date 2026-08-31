using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations
{
    public class ExamConfiguration : AuditableEntityConfiguration<Exam>
    {
        public override void Configure(EntityTypeBuilder<Exam> builder)
        {
            base.Configure(builder);

            builder.ToTable("exams", "dbo");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Id)
                    .HasColumnName("id");

            builder.Property(e => e.ExamTermId)
                    .HasColumnName("exam_term_id")
                    .IsRequired();

            builder.Property(e => e.ClassSubjectId)
                    .HasColumnName("class_subject_id")
                    .IsRequired();

            builder.Property(e => e.ExamDate)
                    .HasColumnName("exam_date")
                    .HasColumnType("date")
                    .IsRequired();

            builder.Property(e => e.StartTime)
                    .HasColumnName("start_time")
                    .IsRequired();

            builder.Property(e => e.EndTime)
                    .HasColumnName("end_time")
                    .IsRequired();

            builder.Property(e => e.TimePeriodId)
                    .HasColumnName("time_period_id")
                    .IsRequired(false);

            builder.HasOne(e => e.TimePeriod)
                    .WithMany()
                    .HasForeignKey(e => e.TimePeriodId)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.Property(e => e.Remarks)
                    .HasColumnName("remarks")
                    .HasMaxLength(500);

            builder.Property(e => e.MarksLocked)
                    .HasColumnName("marks_locked")
                    .HasDefaultValue(false)
                    .IsRequired();

            // Plain scalar lineage column, no FK -- see the doc comment on the entity.
            builder.Property(e => e.CalendarEventId)
                    .HasColumnName("calendar_event_id")
                    .IsRequired(false);

            builder.HasOne(e => e.ExamTerm)
                    .WithMany(t => t.Exams)
                    .HasForeignKey(e => e.ExamTermId)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.ClassSubject)
                    .WithMany()
                    .HasForeignKey(e => e.ClassSubjectId)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(e => e.ExamTermId)
                    .HasDatabaseName("ix_exams_exam_term_id");

            // One exam per subject per term -- the revised design drops per-section scheduling
            // and the free-form Name that used to allow multiple sittings.
            builder.HasIndex(e => new { e.ExamTermId, e.ClassSubjectId })
                    .IsUnique()
                    .HasDatabaseName("ix_exams_term_subject");

            builder.HasIndex(e => e.ExamDate)
                    .HasDatabaseName("ix_exams_exam_date");
        }
    }
}
