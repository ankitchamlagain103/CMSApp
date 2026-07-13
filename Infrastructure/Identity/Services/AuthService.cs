using System.Security.Cryptography;
using Application.Auth;
using Application.Auth.Commands;
using Application.Auth.Dtos;
using Application.Auth.Validators;
using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Constants;
using Domain.Enums;
using FluentValidation.Results;
using Google.Apis.Auth;
using Infrastructure.Common;
using Infrastructure.Email;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Identity.Services
{
    public class AuthService : IAuthService
    {
        private const string GoogleAuthLoginProvider = "Google";

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly JwtTokenGenerator _jwtTokenGenerator;
        private readonly LoginCommandValidator _loginCommandValidator;
        private readonly ChangePasswordCommandValidator _changePasswordCommandValidator;
        private readonly SetPasswordCommandValidator _setPasswordCommandValidator;
        private readonly RefreshTokenCommandValidator _refreshTokenCommandValidator;
        private readonly VerifyEmailCommandValidator _verifyEmailCommandValidator;
        private readonly ResendVerificationEmailCommandValidator _resendVerificationEmailCommandValidator;
        private readonly ForgotPasswordCommandValidator _forgotPasswordCommandValidator;
        private readonly ResetPasswordCommandValidator _resetPasswordCommandValidator;
        private readonly GoogleLoginCommandValidator _googleLoginCommandValidator;
        private readonly ApplicationDbContext _dbContext;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            SignInManager<ApplicationUser> signInManager,
            JwtTokenGenerator jwtTokenGenerator,
            LoginCommandValidator loginCommandValidator,
            ChangePasswordCommandValidator changePasswordCommandValidator,
            SetPasswordCommandValidator setPasswordCommandValidator,
            RefreshTokenCommandValidator refreshTokenCommandValidator,
            VerifyEmailCommandValidator verifyEmailCommandValidator,
            ResendVerificationEmailCommandValidator resendVerificationEmailCommandValidator,
            ForgotPasswordCommandValidator forgotPasswordCommandValidator,
            ResetPasswordCommandValidator resetPasswordCommandValidator,
            GoogleLoginCommandValidator googleLoginCommandValidator,
            ApplicationDbContext dbContext,
            IEmailService emailService,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _jwtTokenGenerator = jwtTokenGenerator;
            _loginCommandValidator = loginCommandValidator;
            _changePasswordCommandValidator = changePasswordCommandValidator;
            _setPasswordCommandValidator = setPasswordCommandValidator;
            _refreshTokenCommandValidator = refreshTokenCommandValidator;
            _verifyEmailCommandValidator = verifyEmailCommandValidator;
            _resendVerificationEmailCommandValidator = resendVerificationEmailCommandValidator;
            _forgotPasswordCommandValidator = forgotPasswordCommandValidator;
            _resetPasswordCommandValidator = resetPasswordCommandValidator;
            _googleLoginCommandValidator = googleLoginCommandValidator;
            _dbContext = dbContext;
            _emailService = emailService;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<CommonResponse<AuthResultDto>> LoginAsync(LoginCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _loginCommandValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<AuthResultDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var invalidCredentialsResponse = CommonResponse<AuthResultDto>.Fail(ResponseCodes.Unauthorized, "Invalid username or password.");

            var user = await _userManager.FindByNameAsync(command.UserName);
            if (user == null)
            {
                return invalidCredentialsResponse;
            }

            // Password is checked before any account-state check (IsActive/EmailConfirmed/expiry) so that
            // an unauthenticated caller can never learn anything about the account from the response --
            // only someone who already knows the correct password gets a specific reason.
            var signInResult = await _signInManager.CheckPasswordSignInAsync(user, command.Password, lockoutOnFailure: true);
            if (signInResult.IsLockedOut)
            {
                var lockedOutResponse = CommonResponse<AuthResultDto>.Fail(ResponseCodes.Unauthorized, "Account is locked due to multiple failed login attempts. Try again later.");
                return lockedOutResponse;
            }

            if (!signInResult.Succeeded)
            {
                return invalidCredentialsResponse;
            }

            if (!user.IsActive)
            {
                var inactiveResponse = CommonResponse<AuthResultDto>.Fail(ResponseCodes.Unauthorized, "This account has been deactivated.");
                return inactiveResponse;
            }

            if (!user.EmailConfirmed)
            {
                var emailNotConfirmedResponse = CommonResponse<AuthResultDto>.Fail(ResponseCodes.Unauthorized, "Please verify your email address before logging in.");
                return emailNotConfirmedResponse;
            }

            // Like the other account-state checks, this runs only after the password is verified,
            // so it reveals nothing to a caller who doesn't already own the account.
            if (!IsRequestIpAllowed(user))
            {
                var ipNotAllowedResponse = CommonResponse<AuthResultDto>.Fail(ResponseCodes.Unauthorized, "Login is not allowed from this IP address.");
                return ipNotAllowedResponse;
            }

            var passwordExpiryDays = _configuration.GetValue<int>("Auth:PasswordExpiryDays");
            if (passwordExpiryDays > 0 && user.LastPasswordChangedTs.HasValue)
            {
                var passwordAge = DateTimeOffset.UtcNow - user.LastPasswordChangedTs.Value;
                if (passwordAge.TotalDays > passwordExpiryDays)
                {
                    var passwordExpiredResponse = CommonResponse<AuthResultDto>.Fail(ResponseCodes.Unauthorized, "Your password has expired. Please reset it before logging in.");
                    return passwordExpiredResponse;
                }
            }

            user.LastLoginTs = DateTimeOffset.UtcNow;
            await _userManager.UpdateAsync(user);

            var authResultDto = await IssueTokensAsync(user, cancellationToken);
            var successResponse = CommonResponse<AuthResultDto>.Success(authResultDto, "Login successful.");
            return successResponse;
        }

        public async Task<CommonResponse<AuthResultDto>> RefreshTokenAsync(RefreshTokenCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _refreshTokenCommandValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<AuthResultDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var invalidTokenResponse = CommonResponse<AuthResultDto>.Fail(ResponseCodes.Unauthorized, "Invalid or expired refresh token.");

            var existingToken = await _dbContext.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == command.RefreshToken, cancellationToken);

            if (existingToken == null)
            {
                return invalidTokenResponse;
            }

            // Reuse detection: a revoked token can only be presented again by a client that
            // shouldn't have it anymore -- most likely it was stolen and the thief (or the victim)
            // is redeeming the rotation chain's older link. Kill every active session for the user
            // rather than just rejecting, so a stolen chain can't stay alive. Skipped for tokens
            // that are also expired: replaying one of those is a stale client, not an attack, and
            // it couldn't have been redeemed anyway. CancellationToken.None -- same reasoning as
            // every other security cleanup.
            if (existingToken.IsRevoked)
            {
                if (!existingToken.IsExpired)
                {
                    await LogoutAsync(existingToken.UserId, CancellationToken.None);
                }

                return invalidTokenResponse;
            }

            if (existingToken.IsExpired)
            {
                return invalidTokenResponse;
            }

            var user = await _userManager.FindByIdAsync(existingToken.UserId.ToString());
            if (user == null || !user.IsActive)
            {
                return invalidTokenResponse;
            }

            // An IP-restricted user must not be able to mint fresh tokens from a disallowed
            // address. Deliberately the same generic message as any other invalid token.
            if (!IsRequestIpAllowed(user))
            {
                return invalidTokenResponse;
            }

            var authResultDto = await IssueTokensAsync(user, cancellationToken);

            // Rotation: the presented token is retired and linked to the one that replaced it,
            // rather than reused -- this token value can never be redeemed again. Revocation runs
            // with CancellationToken.None: once the replacement token has been issued above, a
            // client disconnect must not be able to leave both tokens redeemable.
            existingToken.RevokedAtUtc = DateTime.UtcNow;
            existingToken.ReplacedByToken = authResultDto.RefreshToken;
            await _dbContext.SaveChangesAsync(CancellationToken.None);

            var successResponse = CommonResponse<AuthResultDto>.Success(authResultDto, "Token refreshed successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<bool>> LogoutAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            // Revokes every active session for this user, not just one device -- there's no
            // per-device token supplied to this method, so "logout" means "logout everywhere."
            var activeTokens = await _dbContext.RefreshTokens
                .Where(rt => rt.UserId == userId && rt.RevokedAtUtc == null)
                .ToListAsync(cancellationToken);

            if (activeTokens.Count > 0)
            {
                var revokedAt = DateTime.UtcNow;
                foreach (var token in activeTokens)
                {
                    token.RevokedAtUtc = revokedAt;
                }

                // Deliberately not cancellable: revocation is a security cleanup that must not be
                // abandoned halfway because the caller disconnected.
                await _dbContext.SaveChangesAsync(CancellationToken.None);
            }

            var successResponse = CommonResponse<bool>.Success(true, "Logged out successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<bool>> ChangePasswordAsync(Guid userId, ChangePasswordCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _changePasswordCommandValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<bool>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                var notFoundResponse = CommonResponse<bool>.Fail(ResponseCodes.NotFound, "User was not found.");
                return notFoundResponse;
            }

            user.LastPasswordChangedTs = DateTimeOffset.UtcNow;

            var changeResult = await _userManager.ChangePasswordAsync(user, command.CurrentPassword, command.NewPassword);
            if (!changeResult.Succeeded)
            {
                var errorMessages = new List<string>();
                foreach (var error in changeResult.Errors)
                {
                    errorMessages.Add(error.Description);
                }

                var combinedMessage = string.Join(" ", errorMessages);
                var changeFailureResponse = CommonResponse<bool>.Fail(ResponseCodes.ValidationError, combinedMessage);
                return changeFailureResponse;
            }

            // CancellationToken.None: the password has already changed at this point -- session
            // revocation must run to completion even if the client has disconnected.
            await LogoutAsync(userId, CancellationToken.None);

            var successResponse = CommonResponse<bool>.Success(true, "Password changed successfully. Please log in again.");
            return successResponse;
        }

        public async Task<CommonResponse<bool>> SetPasswordAsync(Guid userId, SetPasswordCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _setPasswordCommandValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<bool>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                var notFoundResponse = CommonResponse<bool>.Fail(ResponseCodes.NotFound, "User was not found.");
                return notFoundResponse;
            }

            // Only for accounts that have no password yet (created via an external login such as
            // Google). An account that already has one must go through change-password, which
            // requires proving knowledge of the current password.
            var hasPassword = await _userManager.HasPasswordAsync(user);
            if (hasPassword)
            {
                var conflictResponse = CommonResponse<bool>.Fail(ResponseCodes.Conflict, "This account already has a password. Use change-password instead.");
                return conflictResponse;
            }

            user.LastPasswordChangedTs = DateTimeOffset.UtcNow;

            var addPasswordResult = await _userManager.AddPasswordAsync(user, command.NewPassword);
            if (!addPasswordResult.Succeeded)
            {
                var errorMessages = new List<string>();
                foreach (var error in addPasswordResult.Errors)
                {
                    errorMessages.Add(error.Description);
                }

                var combinedMessage = string.Join(" ", errorMessages);
                var addFailureResponse = CommonResponse<bool>.Fail(ResponseCodes.ValidationError, combinedMessage);
                return addFailureResponse;
            }

            // Unlike change/reset-password, existing sessions are NOT revoked here: no previous
            // credential was replaced or could have been compromised -- the caller is simply
            // adding a second way to sign in to an account they are already signed in to.
            var successResponse = CommonResponse<bool>.Success(true, "Password set successfully. You can now also log in with your username and password.");
            return successResponse;
        }

        public async Task<CommonResponse<bool>> VerifyEmailAsync(VerifyEmailCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _verifyEmailCommandValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<bool>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var user = await _userManager.FindByIdAsync(command.UserId.ToString());
            if (user == null)
            {
                var notFoundResponse = CommonResponse<bool>.Fail(ResponseCodes.NotFound, "User was not found.");
                return notFoundResponse;
            }

            var confirmResult = await _userManager.ConfirmEmailAsync(user, command.Token);
            if (!confirmResult.Succeeded)
            {
                var invalidTokenResponse = CommonResponse<bool>.Fail(ResponseCodes.ValidationError, "Invalid or expired verification token.");
                return invalidTokenResponse;
            }

            var successResponse = CommonResponse<bool>.Success(true, "Email verified successfully. You can now log in.");
            return successResponse;
        }

        public async Task<CommonResponse<bool>> ResendVerificationEmailAsync(ResendVerificationEmailCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _resendVerificationEmailCommandValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<bool>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            // Always the same generic response, whether or not the email exists or is already
            // confirmed -- this endpoint must never reveal account existence to an anonymous caller.
            var genericResponse = CommonResponse<bool>.Success(true, "If that email exists and is not yet verified, a verification link has been sent.");

            var user = await _userManager.FindByEmailAsync(command.Email);
            if (user == null || user.EmailConfirmed)
            {
                return genericResponse;
            }

            var confirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var verificationLink = EmailLinkBuilder.BuildVerifyEmailLink(_configuration, user.Id, confirmationToken);
            var emailBody = "<p>Please verify your email address:</p><p>" + verificationLink + "</p>";
            await _emailService.SendEmailAsync(user.Email, "Verify your email address", emailBody, cancellationToken);

            return genericResponse;
        }

        public async Task<CommonResponse<bool>> ForgotPasswordAsync(ForgotPasswordCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _forgotPasswordCommandValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<bool>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            // Same reasoning as ResendVerificationEmailAsync -- always the same generic response.
            var genericResponse = CommonResponse<bool>.Success(true, "If that email exists, a password reset link has been sent.");

            var user = await _userManager.FindByEmailAsync(command.Email);
            if (user == null || !user.IsActive || !user.EmailConfirmed)
            {
                return genericResponse;
            }

            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetLink = EmailLinkBuilder.BuildResetPasswordLink(_configuration, user.Id, resetToken);
            var emailBody = "<p>A password reset was requested for your account.</p><p>" + resetLink + "</p><p>This link expires soon -- request a new one if it does.</p>";
            await _emailService.SendEmailAsync(user.Email, "Reset your password", emailBody, cancellationToken);

            return genericResponse;
        }

        public async Task<CommonResponse<bool>> ResetPasswordAsync(ResetPasswordCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _resetPasswordCommandValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<bool>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var user = await _userManager.FindByIdAsync(command.UserId.ToString());
            if (user == null)
            {
                var invalidResponse = CommonResponse<bool>.Fail(ResponseCodes.ValidationError, "Invalid or expired reset token.");
                return invalidResponse;
            }

            var resetResult = await _userManager.ResetPasswordAsync(user, command.Token, command.NewPassword);
            if (!resetResult.Succeeded)
            {
                var errorMessages = new List<string>();
                foreach (var error in resetResult.Errors)
                {
                    errorMessages.Add(error.Description);
                }

                var combinedMessage = string.Join(" ", errorMessages);
                var resetFailureResponse = CommonResponse<bool>.Fail(ResponseCodes.ValidationError, combinedMessage);
                return resetFailureResponse;
            }

            user.LastPasswordChangedTs = DateTimeOffset.UtcNow;
            await _userManager.UpdateAsync(user);

            // CancellationToken.None: same reasoning as ChangePasswordAsync -- the reset already
            // happened, so revoking existing sessions must not be skippable.
            await LogoutAsync(user.Id, CancellationToken.None);

            var successResponse = CommonResponse<bool>.Success(true, "Password reset successfully. Please log in with your new password.");
            return successResponse;
        }

        public async Task<CommonResponse<AuthResultDto>> GoogleLoginAsync(GoogleLoginCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _googleLoginCommandValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<AuthResultDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var invalidTokenResponse = CommonResponse<AuthResultDto>.Fail(ResponseCodes.Unauthorized, "Invalid Google sign-in token.");

            GoogleJsonWebSignature.Payload payload;
            try
            {
                var googleClientId = _configuration["Authentication:Google:ClientId"];
                var validationSettings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { googleClientId }
                };
                payload = await GoogleJsonWebSignature.ValidateAsync(command.IdToken, validationSettings);
            }
            catch (InvalidJwtException)
            {
                return invalidTokenResponse;
            }

            // The Google-issued "sub" claim is the stable, unique identifier for the Google
            // account -- that's what ApplicationUserLogin.ProviderKey is keyed on, not the email
            // (a Google account's email can change; its sub cannot).
            var user = await _userManager.FindByLoginAsync(GoogleAuthLoginProvider, payload.Subject);
            if (user == null)
            {
                user = await _userManager.FindByEmailAsync(payload.Email);
                if (user == null)
                {
                    user = new ApplicationUser
                    {
                        UserName = await GenerateUniqueUserNameAsync(payload.Email),
                        Email = payload.Email,
                        EmailConfirmed = payload.EmailVerified,
                        FirstName = payload.GivenName ?? payload.Email,
                        LastName = payload.FamilyName ?? string.Empty,
                        Gender = Gender.Other,
                        UserType = UserType.User,
                        IsActive = true,
                        IsTosAgreed = true
                    };

                    // No password -- this account can only ever sign in through Google unless the
                    // user later adds one via POST /api/auth/set-password (SetPasswordAsync).
                    var createResult = await _userManager.CreateAsync(user);
                    if (!createResult.Succeeded)
                    {
                        var errorMessages = new List<string>();
                        foreach (var error in createResult.Errors)
                        {
                            errorMessages.Add(error.Description);
                        }

                        var combinedMessage = string.Join(" ", errorMessages);
                        var createFailureResponse = CommonResponse<AuthResultDto>.Fail(ResponseCodes.ValidationError, combinedMessage);
                        return createFailureResponse;
                    }

                    // Same best-effort default role as normal registration (UserService), so a
                    // Google-created account doesn't start with an empty menu tree while a
                    // password-registered one gets the User role.
                    await AssignDefaultRoleAsync(user);
                }
                else if (payload.EmailVerified && !user.EmailConfirmed)
                {
                    user.EmailConfirmed = true;
                }

                var addLoginResult = await _userManager.AddLoginAsync(user, new UserLoginInfo(GoogleAuthLoginProvider, payload.Subject, "Google"));
                if (!addLoginResult.Succeeded)
                {
                    var errorMessages = new List<string>();
                    foreach (var error in addLoginResult.Errors)
                    {
                        errorMessages.Add(error.Description);
                    }

                    var combinedMessage = string.Join(" ", errorMessages);
                    var linkFailureResponse = CommonResponse<AuthResultDto>.Fail(ResponseCodes.ValidationError, combinedMessage);
                    return linkFailureResponse;
                }
            }

            if (!user.IsActive)
            {
                var inactiveResponse = CommonResponse<AuthResultDto>.Fail(ResponseCodes.Unauthorized, "This account has been deactivated.");
                return inactiveResponse;
            }

            // Google has already proven account ownership at this point, so a specific message is
            // as safe here as it is in password login.
            if (!IsRequestIpAllowed(user))
            {
                var ipNotAllowedResponse = CommonResponse<AuthResultDto>.Fail(ResponseCodes.Unauthorized, "Login is not allowed from this IP address.");
                return ipNotAllowedResponse;
            }

            user.LastLoginTs = DateTimeOffset.UtcNow;
            await _userManager.UpdateAsync(user);

            var authResultDto = await IssueTokensAsync(user, cancellationToken);
            var successResponse = CommonResponse<AuthResultDto>.Success(authResultDto, "Login successful.");
            return successResponse;
        }

        // Helper Methods
        // AuthorizedAction enforces the same allowlist on every authenticated request; checking it
        // here too means a disallowed address is refused at token issuance instead of getting a
        // token that 403s everywhere. Same fail-closed semantics as IpAllowlistChecker.
        private bool IsRequestIpAllowed(ApplicationUser user)
        {
            if (!user.IsIpRestricted)
            {
                return true;
            }

            var remoteIpAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress;
            var ipIsAllowed = IpAllowlistChecker.IsAllowed(remoteIpAddress, user.UserIpAllowed);
            return ipIsAllowed;
        }

        private async Task AssignDefaultRoleAsync(ApplicationUser user)
        {
            var defaultRoleExists = await _roleManager.RoleExistsAsync(RoleNames.User);
            if (defaultRoleExists)
            {
                await _userManager.AddToRoleAsync(user, RoleNames.User);
            }
        }
        private async Task<string> GenerateUniqueUserNameAsync(string email)
        {
            var baseUserName = email.Split('@')[0];
            var candidateUserName = baseUserName;
            var suffix = 1;

            while (await _userManager.FindByNameAsync(candidateUserName) != null)
            {
                candidateUserName = baseUserName + suffix;
                suffix++;
            }

            return candidateUserName;
        }
        private async Task<AuthResultDto> IssueTokensAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            var roleNames = await _userManager.GetRolesAsync(user);
            var tokenResult = _jwtTokenGenerator.GenerateToken(user, roleNames);
            var refreshTokenResult = await IssueRefreshTokenAsync(user, cancellationToken);

            var authResultDto = new AuthResultDto
            {
                UserId = user.Id,
                UserName = user.UserName,
                Token = tokenResult.Token,
                ExpiresAtUtc = tokenResult.ExpiresAtUtc,
                RefreshToken = refreshTokenResult.Token,
                RefreshTokenExpiresAtUtc = refreshTokenResult.ExpiresAtUtc,
                Roles = roleNames
            };

            return authResultDto;
        }
        private async Task<(string Token, DateTimeOffset ExpiresAtUtc)> IssueRefreshTokenAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            var refreshTokenExpiryDays = _configuration.GetValue<int>("Jwt:RefreshTokenExpiryDays");
            var refreshTokenValue = GenerateSecureRandomToken();
            var refreshTokenExpiry = DateTime.UtcNow.AddDays(refreshTokenExpiryDays);

            // A new row per issuance (not an upsert) -- this is what allows a user to hold
            // several concurrently-valid refresh tokens, one per logged-in device/session.
            var refreshToken = new RefreshToken
            {
                UserId = user.Id,
                Token = refreshTokenValue,
                ExpiresAtUtc = refreshTokenExpiry
            };

            _dbContext.RefreshTokens.Add(refreshToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return (refreshTokenValue, refreshTokenExpiry);
        }
        private static string GenerateSecureRandomToken()
        {
            var randomBytes = RandomNumberGenerator.GetBytes(64);
            var tokenValue = Convert.ToBase64String(randomBytes);
            return tokenValue;
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
