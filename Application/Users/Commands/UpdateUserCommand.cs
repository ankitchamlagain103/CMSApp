using Domain.Enums;

namespace Application.Users.Commands
{
    public class UpdateUserCommand
    {
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public Gender Gender { get; set; }
        public UserType UserType { get; set; }
        public DateTime? Dob { get; set; }
        public string PhoneCountryCode { get; set; }
        public string PhoneNumber { get; set; }
        public string CountryIso3 { get; set; }
        public bool IsActive { get; set; }
        public bool IsIpRestricted { get; set; }

        // Comma-separated allowed IPs (e.g. "203.0.113.7,2001:db8::1"); only meaningful when
        // IsIpRestricted is true.
        public string UserIpAllowed { get; set; }

        // Deliberately no initializer: null (field omitted) means "leave roles unchanged",
        // an empty list means "remove every role" -- the service branches on that difference.
        public List<Guid> RoleIds { get; set; }
    }
}
