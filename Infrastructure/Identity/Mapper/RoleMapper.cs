using Application.Roles.Dtos;

namespace Infrastructure.Identity.Mapper
{
    public static class RoleMapper
    {
        public static RoleDto ToDto(ApplicationRole role)
        {
            var roleDto = new RoleDto
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description
            };

            return roleDto;
        }
    }
}
