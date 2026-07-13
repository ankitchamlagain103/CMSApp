namespace Application.Auth.Dtos
{
    public class AuthResultDto
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; }
        public string Token { get; set; }
        public DateTimeOffset ExpiresAtUtc { get; set; }
        public string RefreshToken { get; set; }
        public DateTimeOffset RefreshTokenExpiresAtUtc { get; set; }
        public IEnumerable<string> Roles { get; set; } = new List<string>();
    }
}
