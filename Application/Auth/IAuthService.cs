using Application.Auth.Commands;
using Application.Auth.Dtos;
using Application.Common.Models;

namespace Application.Auth
{
    public interface IAuthService
    {
        Task<CommonResponse<AuthResultDto>> LoginAsync(LoginCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<AuthResultDto>> RefreshTokenAsync(RefreshTokenCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> LogoutAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> ChangePasswordAsync(Guid userId, ChangePasswordCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> SetPasswordAsync(Guid userId, SetPasswordCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> VerifyEmailAsync(VerifyEmailCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> ResendVerificationEmailAsync(ResendVerificationEmailCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> ForgotPasswordAsync(ForgotPasswordCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> ResetPasswordAsync(ResetPasswordCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<AuthResultDto>> GoogleLoginAsync(GoogleLoginCommand command, CancellationToken cancellationToken = default);
    }
}
