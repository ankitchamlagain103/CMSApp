using Application.Common.Models;
using Application.Menus;
using Application.Menus.Commands;
using Application.Menus.Dtos;
using Application.Menus.Queries;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MenusController : ControllerBase
    {
        private readonly IMenuService _menuService;

        public MenusController(IMenuService menuService)
        {
            _menuService = menuService;
        }

        [HttpPost]
        public async Task<ActionResult<CommonResponse<MenuDto>>> CreateMenu([FromBody] CreateMenuCommand command, CancellationToken cancellationToken)
        {
            var response = await _menuService.CreateMenuAsync(command, cancellationToken);
            if (response.ResponseCode != ResponseCodes.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<CommonResponse<MenuDto>>> GetMenuById(int id, CancellationToken cancellationToken)
        {
            var response = await _menuService.GetMenuByIdAsync(id, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpGet]
        public async Task<ActionResult<CommonResponse<PaginatedResponse<MenuDto>>>> GetMenus([FromQuery] GetMenusQuery query, CancellationToken cancellationToken)
        {
            var response = await _menuService.GetMenusAsync(query, cancellationToken);
            return Ok(response);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<CommonResponse<MenuDto>>> UpdateMenu(int id, [FromBody] UpdateMenuCommand command, CancellationToken cancellationToken)
        {
            var response = await _menuService.UpdateMenuAsync(id, command, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            if (response.ResponseCode != ResponseCodes.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult<CommonResponse<bool>>> DeleteMenu(int id, CancellationToken cancellationToken)
        {
            var response = await _menuService.DeleteMenuAsync(id, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            if (response.ResponseCode != ResponseCodes.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
    }
}
