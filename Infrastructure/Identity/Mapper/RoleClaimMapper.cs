using Application.Roles.Dtos;

namespace Infrastructure.Identity.Mapper
{
    public static class RoleClaimMapper
    {
        public static RoleClaimDto ToDto(ApplicationRoleClaim roleClaim)
        {
            var roleClaimDto = new RoleClaimDto
            {
                Id = roleClaim.Id,
                RoleId = roleClaim.RoleId,
                MenuId = roleClaim.MenuId,
                MenuCode = roleClaim.Menu?.Code,
                MenuDisplayName = roleClaim.Menu?.DisplayName
            };

            return roleClaimDto;
        }
    }
}
