using Application.Users.Dtos;

namespace Infrastructure.Identity.Mapper
{
    public static class UserClaimMapper
    {
        public static UserClaimDto ToDto(ApplicationUserClaim userClaim)
        {
            var userClaimDto = new UserClaimDto
            {
                Id = userClaim.Id,
                UserId = userClaim.UserId,
                MenuId = userClaim.MenuId,
                MenuCode = userClaim.Menu?.Code,
                MenuDisplayName = userClaim.Menu?.DisplayName
            };

            return userClaimDto;
        }
    }
}
