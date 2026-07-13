using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Infrastructure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Common
{
    public class JwtTokenGenerator
    {
        private readonly IConfiguration _configuration;

        public JwtTokenGenerator(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public JwtTokenResult GenerateToken(ApplicationUser user, IEnumerable<string> roleNames)
        {
            var signingKeyValue = _configuration["Jwt:Key"];
            var issuer = _configuration["Jwt:Issuer"];
            var audience = _configuration["Jwt:Audience"];
            var expiryMinutes = _configuration.GetValue<int>("Jwt:ExpiryMinutes");

            var sessionId = Guid.NewGuid().ToString();
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim("userName", user.UserName),
                new Claim("email".ToString(), user.Email ?? string.Empty),
                new Claim("userType", user.UserType.ToString()),
                new Claim("country", user.CountryIso3 ?? string.Empty),
                new Claim("securityStamp", user.SecurityStamp ?? string.Empty),
                new Claim("sessionId", sessionId)
            };

            foreach (var roleName in roleNames)
            {
                claims.Add(new Claim(ClaimTypes.Role, roleName));
            }

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKeyValue));
            var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
            var expiresAtUtc = DateTime.UtcNow.AddMinutes(expiryMinutes);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expiresAtUtc,
                signingCredentials: signingCredentials);

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenValue = tokenHandler.WriteToken(token);

            var tokenResult = new JwtTokenResult
            {
                Token = tokenValue,
                ExpiresAtUtc = expiresAtUtc
            };

            return tokenResult;
        }
    }

    public class JwtTokenResult
    {
        public string Token { get; set; }
        public DateTime ExpiresAtUtc { get; set; }
    }
}
