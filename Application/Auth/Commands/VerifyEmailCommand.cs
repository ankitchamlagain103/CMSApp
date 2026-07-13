namespace Application.Auth.Commands
{
    public class VerifyEmailCommand
    {
        public Guid UserId { get; set; }
        public string Token { get; set; }
    }
}
