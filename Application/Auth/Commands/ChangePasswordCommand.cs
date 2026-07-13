namespace Application.Auth.Commands
{
    public class ChangePasswordCommand
    {
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
    }
}
