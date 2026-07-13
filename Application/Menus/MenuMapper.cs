using Application.Menus.Dtos;
using Domain.Entities;

namespace Application.Menus
{
    public static class MenuMapper
    {
        public static MenuDto ToDto(Menu menu)
        {
            var menuDto = new MenuDto
            {
                Id = menu.Id,
                Code = menu.Code,
                DisplayName = menu.DisplayName,
                Url = menu.Url,
                Icon = menu.Icon,
                MenuType = menu.MenuType,
                Controller = menu.Controller,
                Action = menu.Action,
                ParentId = menu.ParentId,
                MenuFor = menu.MenuFor,
                Order = menu.Order,
                IsHidden = menu.IsHidden
            };

            return menuDto;
        }
    }
}
