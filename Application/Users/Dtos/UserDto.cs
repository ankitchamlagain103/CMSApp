using Domain.Enums;

namespace Application.Users.Dtos
{
    public class UserDto
    {
        public Guid Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public bool EmailConfirmed { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public Gender Gender { get; set; }
        public UserType UserType { get; set; }
        public DateTime? Dob { get; set; }
        public string PhoneCountryCode { get; set; }
        public string PhoneNumber { get; set; }
        public string CountryIso3 { get; set; }
        public bool IsTosAgreed { get; set; }
        public bool IsActive { get; set; }
        public bool IsIpRestricted { get; set; }
        public string UserIpAllowed { get; set; }
        public DateTimeOffset? LastLoginTs { get; set; }
        public DateTimeOffset? LastPasswordChangedTs { get; set; }
        public List<Guid> RoleIds { get; set; } = new List<Guid>();
    }
}
