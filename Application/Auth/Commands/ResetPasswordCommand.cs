namespace Application.Auth.Commands
{
    public class ResetPasswordCommand
    {
        public Guid UserId { get; set; }
        public string Token { get; set; }
        public string NewPassword { get; set; }
    }
}
