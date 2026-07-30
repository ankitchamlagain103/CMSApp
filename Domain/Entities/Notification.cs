using Domain.Enums;

namespace Domain.Entities
{
    // A system-raised notice for one employee (leave workflow transitions, birthday/anniversary
    // reminders, ...). Hard-deleted -- an ephemeral inbox item, not an audit record; nothing
    // downstream depends on a notification's history surviving. No employee-login exists yet
    // (see Employee.UserId's own doc comment), so this is read/managed through the admin-facing
    // GET/POST /api/employees/{id}/notifications endpoints today, same as every other
    // Employee-scoped sub-resource in this codebase -- not a self-service inbox.
    public class Notification : AuditableEntity
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public NotificationType Type { get; set; }
        public bool IsRead { get; set; }

        public virtual Employee Employee { get; set; }
    }
}
