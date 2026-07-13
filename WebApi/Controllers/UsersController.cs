using Application.Common.Models;
using Application.Users;
using Application.Users.Commands;
using Application.Users.Dtos;
using Application.Users.Queries;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        // Deliberately NOT [AllowAnonymous]: this is an admin-only system with no self-registration.
        // Creating a user requires the USER_CREATE permission (seeded in MenuSeeder), which also
        // makes the caller-supplied RoleIds safe -- only permission-holders can assign roles.
        [HttpPost]
        public async Task<ActionResult<CommonResponse<UserDto>>> CreateUser([FromBody] CreateUserCommand command, CancellationToken cancellationToken)
        {
            var response = await _userService.CreateUserAsync(command, cancellationToken);
            if (response.ResponseCode != ResponseCodes.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<CommonResponse<UserDto>>> GetUserById(Guid id, CancellationToken cancellationToken)
        {
            var response = await _userService.GetUserByIdAsync(id, cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpGet]
        public async Task<ActionResult<CommonResponse<PaginatedResponse<UserDto>>>> GetUsers([FromQuery] GetUsersQuery query, CancellationToken cancellationToken)
        {
            var response = await _userService.GetUsersAsync(query, cancellationToken);
            return Ok(response);
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<CommonResponse<UserDto>>> UpdateUser(Guid id, [FromBody] UpdateUserCommand command, CancellationToken cancellationToken)
        {
            var response = await _userService.UpdateUserAsync(id, command, cancellationToken);
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
        public async Task<ActionResult<CommonResponse<bool>>> DeleteUser(Guid id, CancellationToken cancellationToken)
        {
            var response = await _userService.DeleteUserAsync(id, cancellationToken);
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
