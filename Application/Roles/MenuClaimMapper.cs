using Application.Roles.Dtos;
using Domain.Entities;

namespace Application.Roles
{
    public static class MenuClaimMapper
    {
        public static MenuClaimDto ToDto(Menu menu)
        {
            var menuClaimDto = new MenuClaimDto
            {
                Id = menu.Id,
                Code = menu.Code,
                DisplayName = menu.DisplayName,
                Url = menu.Url,
                Icon = menu.Icon,
                MenuType = menu.MenuType,
                ParentId = menu.ParentId,
                Order = menu.Order,
                IsHidden = menu.IsHidden
            };

            return menuClaimDto;
        }
    }
}
