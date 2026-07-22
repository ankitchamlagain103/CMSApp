using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations
{
    public class MeetingAttendeeConfiguration : AuditableEntityConfiguration<MeetingAttendee>
    {
        public override void Configure(EntityTypeBuilder<MeetingAttendee> builder)
        {
            base.Configure(builder);

            builder.ToTable("meeting_attendees", "dbo");

            builder.HasKey(a => a.Id);

            builder.Property(a => a.Id)
                    .HasColumnName("id");

            builder.Property(a => a.MeetingId)
                    .HasColumnName("meeting_id")
                    .IsRequired();

            builder.Property(a => a.UserId)
                    .HasColumnName("user_id");

            builder.Property(a => a.Email)
                    .HasColumnName("email")
                    .IsRequired()
                    .HasMaxLength(256);

            builder.Property(a => a.Status)
                    .HasColumnName("status")
                    .IsRequired();

            builder.HasOne(a => a.Meeting)
                    .WithMany(m => m.Attendees)
                    .HasForeignKey(a => a.MeetingId)
                    .OnDelete(DeleteBehavior.Cascade);

            // Invitees are keyed by email (UserId is optional), so uniqueness is per
            // (MeetingId, Email) -- deviation from a (MeetingId, UserId) unique pair, which
            // could never hold for multiple external invitees without a user id.
            builder.HasIndex(a => new { a.MeetingId, a.Email })
                    .IsUnique()
                    .HasDatabaseName("ix_meeting_attendees_meeting_email");
        }
    }
}
