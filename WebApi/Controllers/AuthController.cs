using Application.Auth;
using Application.Auth.Commands;
using Application.Auth.Dtos;
using Application.Common.Interfaces;
using Application.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WebApi.Extensions;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting(RateLimitingExtensions.AuthPolicyName)]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ICurrentUserService _currentUserService;

        public AuthController(IAuthService authService, ICurrentUserService currentUserService)
        {
            _authService = authService;
            _currentUserService = currentUserService;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<ActionResult<CommonResponse<AuthResultDto>>> Login([FromBody] LoginCommand command, CancellationToken cancellationToken)
        {
            var response = await _authService.LoginAsync(command, cancellationToken);
            if (response.ResponseCode == ResponseCodes.Unauthorized)
            {
                return Unauthorized(response);
            }

            if (response.ResponseCode != ResponseCodes.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [AllowAnonymous]
        [HttpPost("refresh-token")]
        public async Task<ActionResult<CommonResponse<AuthResultDto>>> RefreshToken([FromBody] RefreshTokenCommand command, CancellationToken cancellationToken)
        {
            var response = await _authService.RefreshTokenAsync(command, cancellationToken);
            if (response.ResponseCode == ResponseCodes.Unauthorized)
            {
                return Unauthorized(response);
            }

            if (response.ResponseCode != ResponseCodes.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpPost("logout")]
        public async Task<ActionResult<CommonResponse<bool>>> Logout(CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId;
            if (currentUserId == null)
            {
                var unauthorizedResponse = CommonResponse<bool>.Fail(ResponseCodes.Unauthorized, "You must be logged in to do this.");
                return Unauthorized(unauthorizedResponse);
            }

            var response = await _authService.LogoutAsync(currentUserId.Value, cancellationToken);
            return Ok(response);
        }

        [HttpPost("change-password")]
        public async Task<ActionResult<CommonResponse<bool>>> ChangePassword([FromBody] ChangePasswordCommand command, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId;
            if (currentUserId == null)
            {
                var unauthorizedResponse = CommonResponse<bool>.Fail(ResponseCodes.Unauthorized, "You must be logged in to do this.");
                return Unauthorized(unauthorizedResponse);
            }

            var response = await _authService.ChangePasswordAsync(currentUserId.Value, command, cancellationToken);
            if (response.ResponseCode != ResponseCodes.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpPost("set-password")]
        public async Task<ActionResult<CommonResponse<bool>>> SetPassword([FromBody] SetPasswordCommand command, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId;
            if (currentUserId == null)
            {
                var unauthorizedResponse = CommonResponse<bool>.Fail(ResponseCodes.Unauthorized, "You must be logged in to do this.");
                return Unauthorized(unauthorizedResponse);
            }

            var response = await _authService.SetPasswordAsync(currentUserId.Value, command, cancellationToken);
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

        [AllowAnonymous]
        [HttpPost("verify-email")]
        public async Task<ActionResult<CommonResponse<bool>>> VerifyEmail([FromBody] VerifyEmailCommand command, CancellationToken cancellationToken)
        {
            var response = await _authService.VerifyEmailAsync(command, cancellationToken);
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

        [AllowAnonymous]
        [HttpPost("resend-verification-email")]
        public async Task<ActionResult<CommonResponse<bool>>> ResendVerificationEmail([FromBody] ResendVerificationEmailCommand command, CancellationToken cancellationToken)
        {
            var response = await _authService.ResendVerificationEmailAsync(command, cancellationToken);
            if (response.ResponseCode != ResponseCodes.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [AllowAnonymous]
        [HttpPost("forgot-password")]
        public async Task<ActionResult<CommonResponse<bool>>> ForgotPassword([FromBody] ForgotPasswordCommand command, CancellationToken cancellationToken)
        {
            var response = await _authService.ForgotPasswordAsync(command, cancellationToken);
            if (response.ResponseCode != ResponseCodes.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [AllowAnonymous]
        [HttpPost("reset-password")]
        public async Task<ActionResult<CommonResponse<bool>>> ResetPassword([FromBody] ResetPasswordCommand command, CancellationToken cancellationToken)
        {
            var response = await _authService.ResetPasswordAsync(command, cancellationToken);
            if (response.ResponseCode != ResponseCodes.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [AllowAnonymous]
        [HttpPost("google")]
        public async Task<ActionResult<CommonResponse<AuthResultDto>>> GoogleLogin([FromBody] GoogleLoginCommand command, CancellationToken cancellationToken)
        {
            var response = await _authService.GoogleLoginAsync(command, cancellationToken);
            if (response.ResponseCode == ResponseCodes.Unauthorized)
            {
                return Unauthorized(response);
            }

            if (response.ResponseCode != ResponseCodes.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
    }
}
