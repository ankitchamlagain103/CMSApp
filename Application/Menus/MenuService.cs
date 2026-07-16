using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Menus.Commands;
using Application.Menus.Dtos;
using Application.Menus.Queries;
using Application.Menus.Validators;
using Domain.Common.Filters;
using Domain.Constants;
using Domain.Entities;
using FluentValidation.Results;

namespace Application.Menus
{
    public class MenuService : IMenuService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly CreateMenuCommandValidator _createMenuCommandValidator;
        private readonly UpdateMenuCommandValidator _updateMenuCommandValidator;
        private readonly GetMenusQueryValidator _getMenusQueryValidator;

        public MenuService(
            IUnitOfWork unitOfWork,
            CreateMenuCommandValidator createMenuCommandValidator,
            UpdateMenuCommandValidator updateMenuCommandValidator,
            GetMenusQueryValidator getMenusQueryValidator)
        {
            _unitOfWork = unitOfWork;
            _createMenuCommandValidator = createMenuCommandValidator;
            _updateMenuCommandValidator = updateMenuCommandValidator;
            _getMenusQueryValidator = getMenusQueryValidator;
        }

        public async Task<CommonResponse<MenuDto>> CreateMenuAsync(CreateMenuCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _createMenuCommandValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<MenuDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var codeAlreadyExists = await _unitOfWork.Menus.CodeExistsAsync(command.Code, cancellationToken);
            if (codeAlreadyExists)
            {
                var conflictMessage = "Menu code '" + command.Code + "' is already in use (possibly by a soft-deleted menu).";
                var conflictResponse = CommonResponse<MenuDto>.Fail(ResponseCodes.Conflict, conflictMessage);
                return conflictResponse;
            }

            var hierarchyError = await ValidateHierarchyAsync(command.MenuType, command.ParentId, cancellationToken);
            if (hierarchyError != null)
            {
                var hierarchyFailureResponse = CommonResponse<MenuDto>.Fail(ResponseCodes.ValidationError, hierarchyError);
                return hierarchyFailureResponse;
            }

            var menu = new Menu
            {
                Code = command.Code,
                DisplayName = command.DisplayName,
                Url = command.Url,
                Icon = command.Icon,
                MenuType = command.MenuType,
                Controller = command.Controller,
                Action = command.Action,
                ParentId = command.ParentId,
                MenuFor = command.MenuFor,
                Order = command.Order,
                IsHidden = command.IsHidden
            };

            await _unitOfWork.Menus.AddAsync(menu, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var menuDto = MenuMapper.ToDto(menu);
            var successResponse = CommonResponse<MenuDto>.Success(menuDto, "Menu created successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<MenuDto>> GetMenuByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var menu = await _unitOfWork.Menus.GetByIdAsync(id, cancellationToken);
            if (menu == null)
            {
                var notFoundMessage = "Menu with id '" + id + "' was not found.";
                var notFoundResponse = CommonResponse<MenuDto>.Fail(ResponseCodes.NotFound, notFoundMessage);
                return notFoundResponse;
            }

            var menuDto = MenuMapper.ToDto(menu);
            var successResponse = CommonResponse<MenuDto>.Success(menuDto);
            return successResponse;
        }

        public async Task<CommonResponse<PaginatedResponse<MenuDto>>> GetMenusAsync(GetMenusQuery query, CancellationToken cancellationToken = default)
        {
            var validationResult = _getMenusQueryValidator.Validate(query);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<PaginatedResponse<MenuDto>>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var filter = new MenuFilter
            {
                MenuType = query.MenuType,
                MenuFor = query.MenuFor,
                Search = query.Search,
                ParentId = query.ParentId,
                IsHidden = query.IsHidden
            };

            var pagedMenus = await _unitOfWork.Menus.GetPagedByFilterAsync(filter, query.Page, query.PageSize, cancellationToken);

            var menuDtos = new List<MenuDto>();
            foreach (var menu in pagedMenus.Items)
            {
                var menuDto = MenuMapper.ToDto(menu);
                menuDtos.Add(menuDto);
            }

            var paginatedResponse = new PaginatedResponse<MenuDto>
            {
                Items = menuDtos,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = pagedMenus.TotalCount
            };

            var successResponse = CommonResponse<PaginatedResponse<MenuDto>>.Success(paginatedResponse);
            return successResponse;
        }

        public async Task<CommonResponse<MenuDto>> UpdateMenuAsync(int id, UpdateMenuCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _updateMenuCommandValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<MenuDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var menu = await _unitOfWork.Menus.GetByIdAsync(id, cancellationToken);
            if (menu == null)
            {
                var notFoundMessage = "Menu with id '" + id + "' was not found.";
                var notFoundResponse = CommonResponse<MenuDto>.Fail(ResponseCodes.NotFound, notFoundMessage);
                return notFoundResponse;
            }

            var codeIsChanging = !string.Equals(menu.Code, command.Code, StringComparison.Ordinal);
            if (codeIsChanging)
            {
                var codeAlreadyExists = await _unitOfWork.Menus.CodeExistsAsync(command.Code, cancellationToken);
                if (codeAlreadyExists)
                {
                    var conflictMessage = "Menu code '" + command.Code + "' is already in use (possibly by a soft-deleted menu).";
                    var conflictResponse = CommonResponse<MenuDto>.Fail(ResponseCodes.Conflict, conflictMessage);
                    return conflictResponse;
                }
            }

            if (command.ParentId.HasValue && command.ParentId.Value == id)
            {
                var selfParentResponse = CommonResponse<MenuDto>.Fail(ResponseCodes.ValidationError, "A menu cannot be its own parent.");
                return selfParentResponse;
            }

            var hierarchyError = await ValidateHierarchyAsync(command.MenuType, command.ParentId, cancellationToken);
            if (hierarchyError != null)
            {
                var hierarchyFailureResponse = CommonResponse<MenuDto>.Fail(ResponseCodes.ValidationError, hierarchyError);
                return hierarchyFailureResponse;
            }

            menu.Code = command.Code;
            menu.DisplayName = command.DisplayName;
            menu.Url = command.Url;
            menu.Icon = command.Icon;
            menu.MenuType = command.MenuType;
            menu.Controller = command.Controller;
            menu.Action = command.Action;
            menu.ParentId = command.ParentId;
            menu.MenuFor = command.MenuFor;
            menu.Order = command.Order;
            menu.IsHidden = command.IsHidden;

            _unitOfWork.Menus.Update(menu);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var menuDto = MenuMapper.ToDto(menu);
            var successResponse = CommonResponse<MenuDto>.Success(menuDto, "Menu updated successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<bool>> DeleteMenuAsync(int id, CancellationToken cancellationToken = default)
        {
            var menu = await _unitOfWork.Menus.GetByIdAsync(id, cancellationToken);
            if (menu == null)
            {
                var notFoundMessage = "Menu with id '" + id + "' was not found.";
                var notFoundResponse = CommonResponse<bool>.Fail(ResponseCodes.NotFound, notFoundMessage);
                return notFoundResponse;
            }

            // Deleting a parent would leave its (still-visible) children pointing at a menu the
            // global !IsDeleted filter hides -- orphans in every tree build. Delete bottom-up.
            var hasChildren = await _unitOfWork.Menus.HasChildrenAsync(id, cancellationToken);
            if (hasChildren)
            {
                var conflictMessage = "Menu with id '" + id + "' still has child menus. Delete its children first.";
                var conflictResponse = CommonResponse<bool>.Fail(ResponseCodes.Conflict, conflictMessage);
                return conflictResponse;
            }

            _unitOfWork.Menus.Remove(menu);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var successResponse = CommonResponse<bool>.Success(true, "Menu deleted successfully.");
            return successResponse;
        }

        private async Task<string> ValidateHierarchyAsync(string menuType, int? parentId, CancellationToken cancellationToken)
        {
            if (menuType == MenuTypes.MainMenu)
            {
                if (parentId.HasValue)
                {
                    return "A " + MenuTypes.MainMenu + " cannot have a parent menu.";
                }

                return null;
            }

            if (!parentId.HasValue)
            {
                return "A " + menuType + " must have a parent menu.";
            }

            var parentMenu = await _unitOfWork.Menus.GetByIdAsync(parentId.Value, cancellationToken);
            if (parentMenu == null)
            {
                return "Parent menu with id '" + parentId.Value + "' was not found.";
            }

            if (menuType == MenuTypes.SubMenu)
            {
                if (parentMenu.MenuType != MenuTypes.MainMenu)
                {
                    return "A " + MenuTypes.SubMenu + "'s parent must be a " + MenuTypes.MainMenu + ".";
                }

                return null;
            }

            var parentIsMainOrSubMenu = parentMenu.MenuType == MenuTypes.MainMenu || parentMenu.MenuType == MenuTypes.SubMenu;
            if (!parentIsMainOrSubMenu)
            {
                return "A " + MenuTypes.Permission + "'s parent must be a " + MenuTypes.MainMenu + " or " + MenuTypes.SubMenu + ".";
            }

            return null;
        }

        private static string BuildValidationErrorMessage(ValidationResult validationResult)
        {
            var errorMessages = new List<string>();
            foreach (var failure in validationResult.Errors)
            {
                errorMessages.Add(failure.ErrorMessage);
            }

            var combinedMessage = string.Join(" ", errorMessages);
            return combinedMessage;
        }
    }
}
