using Application.Common.Models;
using Application.Roles;
using Application.Roles.Commands;
using Application.Roles.Dtos;
using Application.Roles.Queries;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RolesController : ControllerBase
    {
        private readonly IRoleService _roleService;

        public RolesController(IRoleService roleService)
        {
            _roleService = roleService;
        }

        [HttpPost]
        public async Task<ActionResult<CommonResponse<RoleDto>>> CreateRole([FromBody] CreateRoleCommand command, CancellationToken cancellationToken)
        {
            var response = await _roleService.CreateRoleAsync(command, cancellationToken);
            if (response.ResponseCode != ResponseCodes.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<CommonResponse<RoleDto>>> GetRoleById(Guid id, CancellationToken cancellationToken)
        {
            var response = await _roleService.GetRoleByIdAsync(id, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpGet]
        public async Task<ActionResult<CommonResponse<PaginatedResponse<RoleDto>>>> GetRoles([FromQuery] GetRolesQuery query, CancellationToken cancellationToken)
        {
            var response = await _roleService.GetRolesAsync(query, cancellationToken);
            return Ok(response);
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<CommonResponse<RoleDto>>> UpdateRole(Guid id, [FromBody] UpdateRoleCommand command, CancellationToken cancellationToken)
        {
            var response = await _roleService.UpdateRoleAsync(id, command, cancellationToken);
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

        [HttpDelete("{id:guid}")]
        public async Task<ActionResult<CommonResponse<bool>>> DeleteRole(Guid id, CancellationToken cancellationToken)
        {
            var response = await _roleService.DeleteRoleAsync(id, cancellationToken);
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

        [HttpGet("user-menus")]
        public async Task<ActionResult<CommonResponse<List<MenuClaimDto>>>> GetUserRoles(CancellationToken cancellationToken)
        {
            var response = await _roleService.GetUserRolesAsync(cancellationToken);
            if (response.ResponseCode == ResponseCodes.Unauthorized)
            {
                return Unauthorized(response);
            }

            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpPost("users")]
        public async Task<ActionResult<CommonResponse<bool>>> AssignRoleToUser([FromBody] AssignRoleToUserCommand command, CancellationToken cancellationToken)
        {
            var response = await _roleService.AssignRoleToUserAsync(command, cancellationToken);
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

        [HttpDelete("users/{userId:guid}/{roleId:guid}")]
        public async Task<ActionResult<CommonResponse<bool>>> RemoveRoleFromUser(Guid userId, Guid roleId, CancellationToken cancellationToken)
        {
            var response = await _roleService.RemoveRoleFromUserAsync(userId, roleId, cancellationToken);
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

        [HttpGet("{roleId:guid}/claims")]
        public async Task<ActionResult<CommonResponse<List<RoleClaimDto>>>> GetRoleClaims(Guid roleId, CancellationToken cancellationToken)
        {
            var response = await _roleService.GetRoleClaimsAsync(roleId, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpPost("claims")]
        public async Task<ActionResult<CommonResponse<RoleClaimDto>>> AssignMenuToRole([FromBody] AssignMenuToRoleCommand command, CancellationToken cancellationToken)
        {
            var response = await _roleService.AssignMenuToRoleAsync(command, cancellationToken);
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

        [HttpDelete("{roleId:guid}/claims/{menuId:int}")]
        public async Task<ActionResult<CommonResponse<bool>>> RemoveMenuFromRole(Guid roleId, int menuId, CancellationToken cancellationToken)
        {
            var response = await _roleService.RemoveMenuFromRoleAsync(roleId, menuId, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }
    }
}
