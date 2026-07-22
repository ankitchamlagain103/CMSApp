using Application.Users.Dtos;

namespace Infrastructure.Identity.Mapper
{
    public static class UserMapper
    {
        public static UserDto ToDto(ApplicationUser user, List<Guid> roleIds)
        {
            var userDto = new UserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                EmailConfirmed = user.EmailConfirmed,
                FirstName = user.FirstName,
                MiddleName = user.MiddleName,
                LastName = user.LastName,
                Gender = user.Gender,
                UserType = user.UserType,
                Dob = user.Dob,
                PhoneCountryCode = user.PhoneCountryCode,
                PhoneNumber = user.PhoneNumber,
                CountryIso3 = user.CountryIso3,
                IsTosAgreed = user.IsTosAgreed,
                IsActive = user.IsActive,
                IsIpRestricted = user.IsIpRestricted,
                UserIpAllowed = user.UserIpAllowed,
                LastLoginTs = user.LastLoginTs,
                LastPasswordChangedTs = user.LastPasswordChangedTs,
                RoleIds = roleIds,
                CreatedBy = user.CreatedBy,
                CreatedTs = user.CreatedTs,
                UpdatedBy = user.UpdatedBy,
                UpdatedTs = user.UpdatedTs
            };

            return userDto;
        }
    }
}
