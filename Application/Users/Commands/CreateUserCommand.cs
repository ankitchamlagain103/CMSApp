using Domain.Enums;

namespace Application.Users.Commands
{
    public class CreateUserCommand
    {
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public Gender Gender { get; set; }
        public DateTime? Dob { get; set; }
        public string PhoneCountryCode { get; set; }
        public string PhoneNumber { get; set; }
        public string CountryIso3 { get; set; }
        public bool IsTosAgreed { get; set; }
        public List<Guid> RoleIds { get; set; } = new List<Guid>();
    }
}
